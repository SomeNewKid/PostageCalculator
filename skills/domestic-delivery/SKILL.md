---
name: domestic-delivery
description: Calculate postage from Perth, Australia to an Australian destination city. Use this skill when the destination country is Australia.
compatibility: Requires python3 and access to the application Cities.csv data file.
---

# Domestic Delivery

Use this skill to calculate postage for a package sent from Perth, Australia to another city in Australia.

Use this skill only when the destination country is Australia.

Do not use this skill for destinations outside Australia. For destinations outside Australia, use the `international-shipping` skill instead.

## Required Inputs

- `city`: The destination city in Australia.
- `weight`: The package weight in kilograms.

The origin is always Perth, Australia. Do not ask the user for the origin.

## Script

Run `scripts/domestic_postage.py` with command-line arguments in `key=value` form.

Example arguments:

```json
[
  "city=Sydney",
  "weight=2.5"
]
```

These are example values only. Replace `Sydney` and `2.5` with the destination city and weight extracted from the user's current request.

The equivalent command is:

```text
python scripts/domestic_postage.py city=Sydney weight=2.5
```

## Response Guidance

Use the script output as the source of truth for the postage cost.

Tell the user the destination, package weight, and calculated postage cost in Australian dollars.

If the script reports that data is not available for the requested city, explain that the destination is not currently supported.
