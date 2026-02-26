# Product Selection Extraction

Parse the HTML product table below. Extract ONLY checked rows (checked="checked"). Output a JSON array.

[additional_data]

**Output format (strict):**
```json
[
  {
    "productId": "17",
    "quantity": 1,
    "extraData": {
      "searchAttributes": [
        {"name": "Color", "value": "Rose"},
        {"name": "Memory capacity", "value": "64 GB"}
      ]
    }
  }
]
```

**Rules:**
1. productId: from checkbox `value` or `data-product-id` (string)
2. quantity: from `<input type="number">`, default 1
3. extraData.searchAttributes: from each `<select data-attribute-name="X">` where `<option selected>` → `{"name": "X", "value": "selected_value"}`. Empty array `[]` if no attributes.
4. Include ONLY rows with checkbox checked.
5. No products selected → return `[]`

**Return JSON array only.** Enclose between ```json and ```.
