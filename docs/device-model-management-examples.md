**Device Model Management Samples**

`POST /api/devicemodel/add`

```json
{
  "manufacturerId": 3,
  "name": "Queclink GV300W",
  "deviceCategoryId": 2,
  "useIMEIAsPrimaryId": true,
  "deviceNo": "D-001",
  "imeiSerialNumber": "359339080123456",
  "isEnabled": true
}
```

`PUT /api/devicemodel/update`

```json
{
  "id": 14,
  "manufacturerId": 3,
  "name": "Queclink GV300W Pro",
  "deviceCategoryId": 2,
  "useIMEIAsPrimaryId": true,
  "deviceNo": "D-001",
  "imeiSerialNumber": "359339080123456",
  "isEnabled": true
}
```

`GET /api/devicemodel/getall?page=1&pageSize=10&search=Queclink&manufacturerId=3&deviceCategoryId=2&isEnabled=true`

Successful `GET /api/devicemodel/getall` response:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "page": 1,
    "pageSize": 10,
    "totalRecords": 1,
    "totalPages": 1,
    "items": [
      {
        "id": 14,
        "manufacturerId": 3,
        "manufacturerName": "Queclink",
        "name": "Queclink GV300W Pro",
        "deviceCategoryId": 2,
        "deviceCategoryName": "GPS Tracker",
        "useIMEIAsPrimaryId": true,
        "deviceNo": "D-001",
        "imeiSerialNumber": "359339080123456",
        "isEnabled": true,
        "createdAt": "2026-04-30T05:15:00Z",
        "updatedAt": "2026-04-30T06:10:00Z"
      }
    ]
  }
}
```

Successful dropdown response:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Success",
  "data": [
    {
      "id": 3,
      "value": "Queclink"
    },
    {
      "id": 5,
      "value": "Teltonika"
    }
  ]
}
```

Validation error response:

```json
{
  "success": false,
  "statusCode": 400,
  "message": "IMEISerialNumber is required when UseIMEIAsPrimaryId is true.",
  "data": null
}
```
