import { apiClient } from '@/api/client';

export interface MarinaPhotoDto {
  id: string;
  kind: string;
  urlFull: string | null;
  urlMedium: string | null;
  urlThumbnail: string | null;
  sortOrder: number;
  width: number | null;
  height: number | null;
  caption: string | null;
  latitude: number | null;
  longitude: number | null;
  uploadedAt: string;
}

interface UploadTicket {
  uploadUrl: string;
  method: string;
  requiredHeaders: Record<string, string>;
  key: string;
  expiresAt: string;
}

export function usePhotoUpload(marinaId: string) {
  async function upload(params: {
    kind: string;
    file: Blob;
    contentType: string;
    imageWidth?: number;
    imageHeight?: number;
    caption?: string;
    latitude?: number;
    longitude?: number;
  }): Promise<MarinaPhotoDto> {
    // Step 1: get presigned ticket
    const ticketResp = await apiClient.post<UploadTicket>(
      `/marinas/${marinaId}/photos/ticket`,
      {
        kind: params.kind,
        contentType: params.contentType,
        fileSizeBytes: params.file.size,
        imageWidth: params.imageWidth ?? null,
        imageHeight: params.imageHeight ?? null,
      }
    );
    const ticket = ticketResp.data;

    // Step 2: upload directly to storage via the presigned S3 URL
    await fetch(ticket.uploadUrl, {
      method: ticket.method,
      headers: ticket.requiredHeaders,
      body: params.file,
    });

    // Step 3: confirm
    const confirmResp = await apiClient.post<MarinaPhotoDto>(
      `/marinas/${marinaId}/photos/confirm`,
      {
        key: ticket.key,
        kind: params.kind,
        caption: params.caption ?? null,
        latitude: params.latitude ?? null,
        longitude: params.longitude ?? null,
      }
    );
    return confirmResp.data;
  }

  async function getPhotos(): Promise<MarinaPhotoDto[]> {
    const resp = await apiClient.get<MarinaPhotoDto[]>(`/marinas/${marinaId}/photos`);
    return resp.data;
  }

  async function reorder(photoId: string, direction: 'up' | 'down'): Promise<void> {
    await apiClient.patch(`/marinas/${marinaId}/photos/${photoId}/reorder`, { direction });
  }

  async function deletePhoto(photoId: string): Promise<void> {
    await apiClient.delete(`/marinas/${marinaId}/photos/${photoId}`);
  }

  return { upload, getPhotos, reorder, deletePhoto };
}
