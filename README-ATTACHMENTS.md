# Attachments API — Endpoint Flow & Mobile Integration Guide

## Overview

The Attachments system handles file upload/download/update for various media types (images, videos, audio, documents). Files are stored on the server's `wwwroot` directory under paths configured in `appsettings.json` (`UploadPaths` section). All endpoints are protected by `[RoleAuthorize]` and return responses wrapped in `ApiResponse<T>`.

Base URL: `/api/v1/attachments`

---

## Place (Folder) Mapping

The `Place` integer tells the system **which folder** to store/retrieve the file from. This maps to `UploadPaths` configuration values.

| Place | Key               | Typical Use              |
|-------|-------------------|--------------------------|
| 0     | DefaultUserImage  | Default user avatar      |
| 1     | UserImages        | User profile images      |
| 2     | PostImages        | Post images              |
| 3     | PostVideos        | Post videos              |
| 4     | PostDocuments     | Post documents           |
| 5     | ClinicImages      | Clinic photos            |
| 6     | ClinicDocuments   | Clinic documents         |
| 7     | DoctorImages      | Doctor profile photos    |
| 8     | DoctorDocuments   | Doctor documents         |
| 9     | MessageImages     | Chat message images      |
| 10    | MessageVideos     | Chat message videos      |
| 11    | MessageDocuments  | Chat message documents   |
| 12    | MessageAudio      | Chat voice messages      |

---

## MediaType Enum

| Value | Name  |
|-------|-------|
| 0     | Image |
| 1     | Video |
| 2     | Audio |
| 3     | File  |

This determines **validator & storage logic** (allowed extensions, service used).

---

## Endpoints

### 1. Upload Single File

`POST /api/v1/attachments/upload`

**Content-Type:** `multipart/form-data`

| Field      | Type      | Description                           |
|------------|-----------|---------------------------------------|
| `File`     | `IFormFile` | The file to upload                   |
| `Place`    | `int`     | Target folder (0–12, see table above) |
| `FileType` | `int`     | `MediaType` enum value (0–3)          |

**Flow:**
```
Controller → UploadFileCommand → Validator (file not empty, Place 0–12, FileType valid enum)
                                 → Handler → Router to correct validator by MediaType:
                                    - Image  → ImageValidator.UploadImage  (allowed: .jpg,.jpeg,.png,.gif,.bmp,.webp)
                                    - Video  → VideoValidator.UploadVideo  (allowed: .mp4,.avi,.mkv,.mov,.wmv)
                                    - Audio  → AudioValidator.UploadAudio  (allowed: .mp3,.wav,.ogg,.m4a,.aac)
                                    - File   → FileValidator.UploadFile    (allowed: .pdf,.doc,.docx,.xls,.xlsx,.txt,.zip,.rar)
                                  → BaseFileService.UploadFileAsync → saves to wwwroot/{folderPath}/{guid}.ext
                                  → Returns "{Place}_{guid}.ext" string
```

**Example Response (200):**
```json
{
  "success": true,
  "data": "5_3a1f2b4c-...jpg",
  "message": "...",
  "statusCode": 200
}
```

**Example Response (400 — validation error):**
```json
{
  "success": false,
  "errors": { "File": ["File is required"] },
  "data": null,
  "statusCode": 400
}
```

---

### 2. Update (Replace) File

`PATCH /api/v1/attachments/update/{name}`

**Content-Type:** `multipart/form-data`

| Field      | Type      | Description                           |
|------------|-----------|---------------------------------------|
| `File`     | `IFormFile` | The new file                        |
| `Place`    | `int`     | Target folder (0–12)                  |
| `FileType` | `int`     | `MediaType` enum value (0–3)          |

The `{name}` path parameter is the **old file name** (e.g. `"5_old-guid.jpg"`).

**Flow:**
```
Controller → extracts {name} → sets command.OldFileName = name
          → UpdateFileCommand → Validator (same as upload)
                               → Handler → Deletes old file via correct validator's Delete* method
                                          → Uploads new file (same as upload flow)
                                          → Returns "{Place}_{new-guid}.ext"
```

**Note:** The old file name is parsed to extract the actual GUID (everything after the first `_`).

---

### 3. Upload Multiple Attachments

`POST /api/v1/attachments/upload-multiple-attachments`

**Content-Type:** `multipart/form-data`

| Field           | Type          | Description                    |
|-----------------|---------------|--------------------------------|
| `Images`        | `List<IFormFile>` | Multiple image files        |
| `ImagesPlace`   | `int`         | Place for images (0–12)        |
| `Videos`        | `List<IFormFile>` | Multiple video files        |
| `VideosPlace`   | `int`         | Place for videos (0–12)        |
| `Audios`        | `List<IFormFile>` | Multiple audio files        |
| `AudiosPlace`   | `int`         | Place for audios (0–12)        |
| `Documents`     | `List<IFormFile>` | Multiple document files    |
| `DocumentsPlace`| `int`         | Place for documents (0–12)     |

**Rule:** At least one file group must be non-empty.

**Flow:**
```
Controller → UploadMultipleAttachmentsCommand → Validator (at least one group has files, all Places 0–12)
                                              → Handler → For each non-null group:
                                                           Images   → ImageValidator.UploadMultipleImage
                                                           Videos   → VideoValidator.UploadMultipleVideo
                                                           Audios   → AudioValidator.UploadAudio (one by one)
                                                           Documents→ FileValidator.UploadMultipleFile
                                              → Returns List<string> of all resulting filenames
```

---

### 4. Download File

`POST /api/v1/attachments/download`

**Content-Type:** `multipart/form-data`

| Field      | Type     | Description                    |
|------------|----------|--------------------------------|
| `Place`    | `int`    | Folder where file is stored    |
| `FileName` | `string` | File name (e.g. `"5_guid.jpg"`)|

**Flow:**
```
Controller → DownloadFileCommand → Validator (FileName not empty, Place 0–12)
                                  → Handler → FileValidator.DownloadFile → BaseFileService.DownloadFileAsync
                                             → Checks if wwwroot/{folderPath}/{fileName} exists
                                             → Detects MIME type via FileExtensionContentTypeProvider
                                             → Returns FileResponseDto
```

**Example Response (200):**
```json
{
  "success": true,
  "data": {
    "filePath": "C:\\...\\wwwroot\\uploads\\images\\guid.jpg",
    "fileName": "guid.jpg",
    "contentType": "image/jpeg",
    "success": true,
    "errorMessage": null
  },
  "statusCode": 200
}
```

**Example Response (400 — file not found):**
```json
{
  "success": true,
  "data": {
    "filePath": "",
    "fileName": "",
    "contentType": "application/octet-stream",
    "success": false,
    "errorMessage": "File not found"
  },
  "statusCode": 200
}
```

---

## Mobile App Integration

### General Rules

1. **Authentication:** All endpoints require a valid JWT token in the `Authorization: Bearer <token>` header.
2. **Base URL:** `https://your-api-domain.com/api/v1`
3. **File Naming Convention:** Returned filenames follow the pattern `"{Place}_{guid}.ext"`. To construct a **full URL** for accessing the file via the custom file provider, use:
   ```
   https://your-api-domain.com/files/{folderPath}/{Place}_{guid}.ext
   ```
4. **Localization:** Set the `Accept-Language` header to `ar` (default) or `en` for localized response messages.

### Example: Upload a Single Image (Kotlin/OkHttp)

```kotlin
suspend fun uploadImage(token: String, imageUri: Uri, place: Int): String {
    val client = OkHttpClient()
    val requestBody = MultipartBody.Builder()
        .setType(MultipartBody.FORM)
        .addFormDataPart("Place", place.toString())
        .addFormDataPart("FileType", "0") // MediaType.Image
        .addFormDataPart("File", "photo.jpg",
            RequestBody.create(MediaType.parse("image/jpeg"), File(imageUri.path!!)))
        .build()

    val request = Request.Builder()
        .url("https://your-api-domain.com/api/v1/attachments/upload")
        .addHeader("Authorization", "Bearer $token")
        .post(requestBody)
        .build()

    val response = client.newCall(request).await()
    val body = response.body()?.string()
    // Parse JSON, extract data field — that's your filename
    return parseFileName(body!!)
}
```

### Example: Upload Multiple Attachments (Swift/Alamofire)

```swift
func uploadMultiple(token: String,
                    images: [UIImage],
                    videos: [URL],
                    completion: @escaping ([String]) -> Void) {
    let headers: HTTPHeaders = [
        "Authorization": "Bearer \(token)"
    ]

    AF.upload(
        multipartFormData: { formData in
            formData.add("0", withName: "ImagesPlace")
            formData.add("3", withName: "VideosPlace")

            for (i, image) in images.enumerated() {
                if let data = image.jpegData(compressionQuality: 0.8) {
                    formData.append(data, withName: "Images",
                                    fileName: "image_\(i).jpg",
                                    mimeType: "image/jpeg")
                }
            }

            for url in videos {
                formData.append(url, withName: "Videos")
            }
        },
        to: "https://your-api-domain.com/api/v1/attachments/upload-multiple-attachments",
        headers: headers
    ).responseDecodable(of: ApiResponse<[String]>.self) { response in
        completion(response.value?.data ?? [])
    }
}
```

### Example: Download File Info

```dart
Future<FileResponseDto?> downloadFile({
  required String token,
  required int place,
  required String fileName,
}) async {
  final request = http.MultipartRequest(
    'POST',
    Uri.parse('https://your-api-domain.com/api/v1/attachments/download'),
  );

  request.headers['Authorization'] = 'Bearer $token';
  request.fields['Place'] = place.toString();
  request.fields['FileName'] = fileName;

  final streamedResponse = await request.send();
  final response = await http.Response.fromStream(streamedResponse);
  return FileResponseDto.fromJson(jsonDecode(response.body));
}
```

### Example: Update/Replace a File (Android/Retrofit)

```kotlin
interface AttachmentApi {
    @Multipart
    @PATCH("attachments/update/{name}")
    suspend fun updateFile(
        @Header("Authorization") token: String,
        @Path("name") oldFileName: String,
        @Part("Place") place: RequestBody,
        @Part("FileType") fileType: RequestBody,
        @Part file: MultipartBody.Part
    ): Response<ApiResponse<String>>
}

// Usage
val oldFileName = "5_old-guid.jpg" // returned from previous upload
val requestBody = "photo.jpg".toRequestBody("image/jpeg".toMediaType())
val part = MultipartBody.Part.createFormData("File", "photo.jpg", requestBody)
val response = api.updateFile(
    token = "Bearer $jwt",
    oldFileName = oldFileName,
    place = "5".toRequestBody(),
    fileType = "0".toRequestBody(),
    file = part
)
```

---

## Architecture Summary (Clean Architecture Layers)

```
┌─────────────────────────────────────────────────────────────────┐
│  API Layer (Controller)                                         │
│  - Receives HTTP request, extracts auth, calls MediatR          │
│  - Returns ApiResponse<T> wrapped result                        │
├─────────────────────────────────────────────────────────────────┤
│  Application Layer (Commands, Handlers, Validators, Interfaces) │
│  - CQRS commands/handlers with validation pipeline              │
│  - IImageValidator, IVideoValidator, IAudioValidator,           │
│    IFileValidator interfaces                                    │
│  - UploadPaths static class maps Place→folder path              │
├─────────────────────────────────────────────────────────────────┤
│  Infrastructure Layer (Implementations)                         │
│  - ImageValidator, VideoValidator, AudioValidator, FileValidator│
│    - Validate file extension                                    │
│    - Call BaseFileService for actual storage                    │
│  - BaseFileService: reads/writes to wwwroot/{folderPath}        │
│  - File named as {Place}_{guid}.ext                             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Configuration (appsettings.json)

```json
{
  "UploadPaths": {
    "DefaultUserImage": "uploads/default",
    "UserImages": "uploads/users",
    "PostImages": "uploads/posts/images",
    "PostVideos": "uploads/posts/videos",
    "PostDocuments": "uploads/posts/documents",
    "ClinicImages": "uploads/clinics/images",
    "ClinicDocuments": "uploads/clinics/documents",
    "DoctorImages": "uploads/doctors/images",
    "DoctorDocuments": "uploads/doctors/documents",
    "MessageImages": "uploads/messages/images",
    "MessageVideos": "uploads/messages/videos",
    "MessageDocuments": "uploads/messages/documents",
    "MessageAudio": "uploads/messages/audio"
  }
}
```

Files are served at runtime via custom `/files` route that maps to `wwwroot/{folderPath}`.
