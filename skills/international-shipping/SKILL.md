---
name: international-shipping
description: Calculate postage from Perth, Australia to a supported destination outside Australia. Use this skill when the destination country is not Australia.
compatibility: Requires python3 and access to the application Cities.csv data file.
---

# International Shipping

Use this skill to calculate postage for a package sent from Perth, Australia to a supported destination outside Australia.

Use this skill only when the destination country is not Australia.

Do not use this skill for destinations in Australia. For Australian destinations, use the `domestic-delivery` skill instead.

## Required Inputs

- `city`: The destination city outside Australia.
- `country`: The destination country.
- `weight`: The package weight in kilograms.

The origin is always Perth, Australia. Do not ask the user for the origin.

## Script

Run `scripts/international_shipping.py` with command-line arguments in `key=value` form.

Example arguments:

```json
[
  "city=Wellington",
  "country=New Zealand",
  "weight=2.5"
]
```

These are example values only. Replace `Wellington`, `New Zealand`, and `2.5` with the destination city, destination country, and weight extracted from the user's current request.

The equivalent command is:

```text
python scripts/international_shipping.py city=Wellington country="New Zealand" weight=2.5
```

## Response Guidance

Use the script output as the source of truth for the postage cost.

Tell the user the destination, package weight, and calculated postage cost in Australian dollars.

If the script reports that data is not available for the requested city and country, explain that the international destination is not currently supported.
