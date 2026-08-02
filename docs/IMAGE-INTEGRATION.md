# Image Integration Guide (Frontend)

## Overview

Images are never stored as full URLs or base64 in the database. Instead, a **two-step process** is used:

1. **Upload** the image binary to get a filename
2. **Store** the filename on the entity (e.g., `doctorImage`, `logo`, `profilePictureUrl`)

The server resolves the filename back to the correct folder at serving time via `/files/{filename}`.

---

## Step 1: Upload the Image

**Endpoint:** `POST /api/v1/attachments/upload`

**Request:** `multipart/form-data`

| Field | Type | Description |
|-------|------|-------------|
| `File` | file | The image binary |
| `Place` | integer | Numeric code for the target folder (see table below) |
| `FileType` | integer | `0` for Image |

**Place codes:**

| Code | Path | Used For |
|------|------|----------|
| `1` | `User/Images` | Profile pictures, staff images |
| `5` | `Clinic/Images` | Clinic logo / image |
| `7` | `Doctor/Images` | Doctor profile images |
| `13` | `Specialization/Icons` | Specialization icons |

**Response:** Returns the filename string, e.g. `1_abc123.jpg` or `7_def456.png`.

### Example (JavaScript)

```js
const formData = new FormData();
formData.append('File', fileBlob);
formData.append('Place', '1');   // User/Images
formData.append('FileType', '0'); // Image

const res = await fetch('/api/v1/attachments/upload', {
  method: 'POST',
  body: formData,
});
const filename = await res.text(); // "1_abc123.jpg"
```

### Example (Flutter)

```dart
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

Future<String> uploadImage(File image) async {
  var request = http.MultipartRequest(
    'POST',
    Uri.parse('https://api.clinichub.com/api/v1/attachments/upload'),
  );
  request.files.add(await http.MultipartFile.fromPath(
    'File',
    image.path,
    contentType: MediaType('image', 'jpeg'),
  ));
  request.fields['Place'] = '1';
  request.fields['FileType'] = '0';
  var response = await request.send();
  return await response.stream.bytesToString();
}
```

---

## Step 2: Send the Filename in Your Request

Once you have the filename, include it in the appropriate entity create/update request:

### Creating Staff

```json
{
  "fullName": "John Doe",
  "email": "john@clinic.com",
  "phoneNumber": "01012345678",
  "password": "P@ssw0rd",
  "image": "1_abc123.jpg"
}
```

### Creating/Updating Doctor

```json
{
  "doctorImage": "7_def456.jpg",
  ...
}
```

---

## Step 3: Display the Image

Construct the URL as:

```
https://api.clinichub.com/files/{filename}
```

**Examples:**

- `https://api.clinichub.com/files/1_abc123.jpg` — profile picture
- `https://api.clinichub.com/files/7_def456.jpg` — doctor image
- `https://api.clinichub.com/files/5_xyz789.png` — clinic logo

The server's `CustomFileProvider` parses the numeric prefix (`1_`, `7_`, `5_`) to determine the correct subfolder and serves the file.

### Display in HTML

```html
<img src="https://api.clinichub.com/files/1_abc123.jpg" alt="Profile" />
```

### Display in Flutter

```dart
Image.network('https://api.clinichub.com/files/1_abc123.jpg')
```

---

## Response Fields Containing Filenames

| Response Object | Field | Example |
|----------------|-------|---------|
| Login / Signup response | `profilePictureUrl` | `"1_abc123.jpg"` |
| Doctor details | `profilePictureUrl` | `"7_def456.jpg"` |
| Clinic details | `logo`, `imageUrl` | `"5_xyz789.png"` |
| Staff list | `image` | `"1_abc123.jpg"` |
| Conversation list | `initiatorProfilePictureUrl`, `recipientProfilePictureUrl` | `"1_abc123.jpg"` |
| Chat messages | `senderProfilePictureUrl` | `"1_abc123.jpg"` |

---

## Updating / Replacing an Image

Use `PATCH /api/v1/attachments/update/{name}` with the old filename to replace an image. The server deletes the old file and uploads the new one, returning the new filename.

---

## Allowed Image Formats

`.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`
