# Address and Geo API Guide

This document describes the shared address registry and geographic data API for use by affiliate sites.

## Overview

The Address and Geo API provides:
- **Geographic reference data**: Countries, provinces/states, cities with GPS
- **Shared address registry**: Standalone addresses not tied to users
- **GPS data for LBS**: Coordinates for location-based services

Affiliate sites link to address IDs and cache GPS coordinates locally for fast LBS queries.

## API Scopes

| Scope | Description |
|-------|-------------|
| `geo:read` | Read countries, provinces/states, cities |
| `geo:write` | Create/update cities with GPS |
| `addresses:read` | Read addresses and GPS data |
| `addresses:write` | Create/update addresses |

## Geographic Data Endpoints

### Countries

**Get all countries:**
```bash
GET /geo/countries
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "United States",
    "code2": "US",
    "code3": "USA",
    "numericCode": "840",
    "phoneCode": "+1",
    "sortOrder": 0
  }
]
```

**Get single country:**
```bash
GET /geo/countries/{id}
```

### Provinces/States

**Get provinces for a country:**
```bash
GET /geo/countries/{countryId}/provinces
```

**Response:**
```json
[
  {
    "id": 5,
    "countryId": 1,
    "name": "California",
    "code": "CA",
    "type": "State",
    "sortOrder": 0
  }
]
```

**Get single province with country info:**
```bash
GET /geo/provinces/{id}
```

### Cities

**Get cities for a province:**
```bash
GET /geo/provinces/{provinceId}/cities
```

**Response:**
```json
[
  {
    "id": 123,
    "provinceStateId": 5,
    "name": "Los Angeles",
    "latitude": 34.052235,
    "longitude": -118.243683
  }
]
```

**Search cities (autocomplete):**
```bash
GET /geo/cities/search?query=Los&countryId=1&limit=10
```

**Response:**
```json
[
  {
    "id": 123,
    "name": "Los Angeles",
    "latitude": 34.052235,
    "longitude": -118.243683,
    "provinceStateId": 5,
    "provinceStateName": "California",
    "provinceStateCode": "CA",
    "countryId": 1,
    "countryName": "United States",
    "countryCode": "US"
  }
]
```

**Create city (user-added):**
```bash
POST /geo/cities
{
  "provinceStateId": 5,
  "name": "New City",
  "latitude": 34.123456,
  "longitude": -118.654321
}
```

**Update city GPS:**
```bash
PUT /geo/cities/{id}/gps
{
  "latitude": 34.123456,
  "longitude": -118.654321
}
```

## Address Endpoints

### Create Address

Creates a new address and returns ID + GPS for local caching.

```bash
POST /addresses
{
  "cityId": 123,
  "line1": "123 Main Street",
  "line2": "Suite 100",
  "postalCode": "90001",
  "latitude": 34.052235,
  "longitude": -118.243683
}
```

**Response:**
```json
{
  "id": 456,
  "latitude": 34.052235,
  "longitude": -118.243683,
  "isVerified": true,
  "gpsSource": "address"
}
```

The `gpsSource` field indicates where GPS came from:
- `"address"` - Precise coordinates provided for this address
- `"city"` - Fallback to city center coordinates
- `"none"` - No GPS available

### Get Address

Returns full address with location hierarchy.

```bash
GET /addresses/{id}
```

**Response:**
```json
{
  "id": 456,
  "line1": "123 Main Street",
  "line2": "Suite 100",
  "postalCode": "90001",
  "latitude": 34.052235,
  "longitude": -118.243683,
  "isVerified": true,
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": null,
  "cityId": 123,
  "cityName": "Los Angeles",
  "cityLatitude": 34.052235,
  "cityLongitude": -118.243683,
  "provinceStateId": 5,
  "provinceStateName": "California",
  "provinceStateCode": "CA",
  "countryId": 1,
  "countryName": "United States",
  "countryCode": "US"
}
```

### Update Address

```bash
PUT /addresses/{id}
{
  "line1": "456 New Street",
  "postalCode": "90002"
}
```

### Update Address GPS Only

```bash
PUT /addresses/{id}/gps
{
  "latitude": 34.052235,
  "longitude": -118.243683,
  "isVerified": true
}
```

### Lookup Address (Avoid Duplicates)

```bash
GET /addresses/lookup?cityId=123&line1=123 Main&postalCode=90001
```

### Quick GPS Lookup

For LBS caching - returns just GPS data.

```bash
GET /addresses/{id}/gps
```

**Response:**
```json
{
  "id": 456,
  "latitude": 34.052235,
  "longitude": -118.243683,
  "isVerified": true,
  "gpsSource": "address"
}
```

### Batch GPS Lookup

```bash
POST /addresses/gps/batch
[456, 457, 458]
```

**Response:**
```json
[
  { "id": 456, "latitude": 34.052235, "longitude": -118.243683, "isVerified": true, "gpsSource": "address" },
  { "id": 457, "latitude": 40.712776, "longitude": -74.005974, "isVerified": false, "gpsSource": "city" },
  { "id": 458, "latitude": null, "longitude": null, "isVerified": false, "gpsSource": "none" }
]
```

## Recommended Usage Pattern

### 1. Create/Find Address via API

```javascript
// Your site's backend
async function getOrCreateAddress(addressData) {
  // First, try to find existing address
  const existing = await api.get('/addresses/lookup', {
    params: {
      cityId: addressData.cityId,
      line1: addressData.line1,
      postalCode: addressData.postalCode
    }
  });

  if (existing.data.length > 0) {
    return existing.data[0];
  }

  // Create new address
  const created = await api.post('/addresses', addressData);
  return created.data;
}
```

### 2. Store Address ID + GPS Locally

```sql
-- Your site's Orders table
CREATE TABLE Orders (
    Id INT PRIMARY KEY,
    CustomerId INT,
    ShippingAddressId INT,     -- Reference to shared address
    ShippingLat DECIMAL(9,6),  -- Cached for fast LBS
    ShippingLng DECIMAL(9,6),
    -- ... other fields
);

-- When creating order
INSERT INTO Orders (CustomerId, ShippingAddressId, ShippingLat, ShippingLng)
VALUES (@CustomerId, @AddressId, @Latitude, @Longitude);
```

### 3. Run LBS Queries Locally

```sql
-- Find orders within 10km of a location (fast, uses local data)
SELECT o.*
FROM Orders o
WHERE dbo.fn_CalculateDistance(@lat, @lng, o.ShippingLat, o.ShippingLng) <= 10
ORDER BY dbo.fn_CalculateDistance(@lat, @lng, o.ShippingLat, o.ShippingLng);
```

### 4. Display Full Address When Needed

```javascript
// When showing address to user, fetch full details
const address = await api.get(`/addresses/${order.shippingAddressId}`);
console.log(`${address.line1}, ${address.cityName}, ${address.provinceStateCode}`);
```

## City Creation Flow

When a user enters a city that doesn't exist:

```javascript
// 1. Search for existing city
const cities = await api.get('/geo/cities/search', {
  params: { query: userInput, countryId: selectedCountry }
});

if (cities.data.length > 0) {
  // 2. Show autocomplete suggestions
  showSuggestions(cities.data);
} else {
  // 3. Allow user to create new city
  const newCity = await api.post('/geo/cities', {
    provinceStateId: selectedProvince,
    name: userInput,
    latitude: null,  // Can be added later
    longitude: null
  });
}
```

## Database Setup

Run the SQL script to create tables:

```bash
sqlcmd -S your-server -d your-database -i database/geo_and_addresses.sql
```

Then import your Countries and ProvinceStates data. Cities will be populated as users add them through the UI.

## Distance Calculation

The `fn_CalculateDistance` function uses the Haversine formula to calculate distance in kilometers:

```sql
-- Example: Find addresses within 10km
SELECT a.*, dbo.fn_CalculateDistance(40.7128, -74.0060,
    COALESCE(a.Latitude, c.Latitude),
    COALESCE(a.Longitude, c.Longitude)) AS DistanceKm
FROM Addresses a
JOIN Cities c ON a.CityId = c.Id
WHERE dbo.fn_CalculateDistance(40.7128, -74.0060,
    COALESCE(a.Latitude, c.Latitude),
    COALESCE(a.Longitude, c.Longitude)) <= 10
ORDER BY DistanceKm;
```
