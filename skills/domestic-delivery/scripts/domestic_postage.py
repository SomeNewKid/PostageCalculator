
import csv
import sys

from pathlib import Path

def calculate_postage(city: str, country: str, weight: float) -> float:
	"""
	Calculate the postage cost based on city, country, and weight.
	Args:
		city (str): The destination city.
		country (str): The destination country.
		weight (float): The weight of the package in kilograms.
	Returns:
		float: The calculated postage cost.
	"""

	originLatitude, originLongitude = _get_coordiates("Perth", "Australia")
	targetLatitude, targetLongitude = _get_coordiates(city, country)

	targetIsPerth = city.lower() == "perth" and country.lower() == "australia"
	distance = 10 if targetIsPerth else _calculate_distance(originLatitude, originLongitude, targetLatitude, targetLongitude)

	base_rate = 5.0  # Base rate for domestic postage
	weight_rate = 2  # Rate per kilogram
	multiplier = 1 if targetIsPerth else 2

	weight_cost = weight * weight_rate * multiplier
	distance_cost = distance * 0.01

	print(f"weight_cost: {weight_cost}, distance_cost: {distance_cost}, base_rate: {base_rate}")

	postage = base_rate + weight_cost + distance_cost

	return postage


def _get_coordiates(city: str, country: str) -> tuple[float, float]:
	cwd = Path.cwd()
	data_file = cwd / "data" / "Cities.csv"
	if not data_file.exists():
		raise FileNotFoundError(f"Data file not found: {data_file}")

	with open(data_file, mode='r', newline='', encoding='utf-8') as csvfile:
		reader = csv.DictReader(csvfile)
		for row in reader:
			if row['city'].lower() == city.lower() and row['country'].lower() == country.lower():
				return float(row['latitude']), float(row['longitude'])

	raise RuntimeError(f"Data not available for {city} in {country}")


def _calculate_distance(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
	"""
	Calculate the distance between two geographical coordinates using the Haversine formula.
	Args:
		lat1 (float): Latitude of the first point in decimal degrees.
		lon1 (float): Longitude of the first point in decimal degrees.
		lat2 (float): Latitude of the second point in decimal degrees.
		lon2 (float): Longitude of the second point in decimal degrees.
	Returns:
		float: The distance between the two points in kilometers.
	"""
	from math import radians, sin, cos, sqrt, atan2
	R = 6371.0  # Radius of the Earth in kilometers
	dlat = radians(lat2 - lat1)
	dlon = radians(lon2 - lon1)
	a = sin(dlat / 2)**2 + cos(radians(lat1)) * cos(radians(lat2)) * sin(dlon / 2)**2
	c = 2 * atan2(sqrt(a), sqrt(1 - a))
	distance = R * c
	return distance


def _parse_cli_args(argv: list[str]) -> tuple[str, float]:
	"""
	Parse command-line arguments.

	Supported forms:
	- key=value style: city=Sydney weight=2.5
	 — Values that contain spaces should be quoted by the shell, e.g. city="Hong Kong"
	- positional style: <city> <weight>

	Returns: (city, country, weight)
	"""
	args = argv[1:]
	parsed: dict[str, str] = {}
	positionals: list[str] = []

	for token in args:
		if '=' in token:
			key, val = token.split('=', 1)
			val = val.strip()
			# strip surrounding quotes if present
			if (val.startswith('"') and val.endswith('"')) or (val.startswith("'") and val.endswith("'")):
				val = val[1:-1]
			parsed[key.lower()] = val
		else:
			positionals.append(token)

	city = parsed.get('city') or (positionals[0] if len(positionals) > 0 else None)
	weight_str = parsed.get('weight') or (positionals[1] if len(positionals) > 1 else None)

	if not city:
		raise ValueError("city not provided")
	if not weight_str:
		raise ValueError("weight not provided")		

	try:
		weight = float(weight_str)
	except ValueError as ex:
		raise ValueError(f"Invalid weight value: {weight_str}") from ex

	return city, weight


def main():
	try:
		city, weight = _parse_cli_args(sys.argv)
	except Exception as e:
		print(f"Error parsing arguments: {e}")
		print('Usage examples:')
		print('  python domestic_postage.py city=Sydney weight=2.5')
		return

	try:
		country = "Australia"
		postage = calculate_postage(city, country, weight)
		print(f"The postage cost to {city}, {country} for a package weighing {weight} kg is: ${postage:.2f}")
	except Exception as e:
		print(f"Failed to calculate postage: {e}")


if __name__ == "__main__":
	main()