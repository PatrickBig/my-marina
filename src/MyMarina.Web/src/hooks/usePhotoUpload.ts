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

export function usePhotoUpload(marinaId: string) {
  async function upload(params: {
    kind: string;
    file: Blob;
    contentType: string;
    caption?: string;
    latitude?: number;
    longitude?: number;
  }): Promise<MarinaPhotoDto> {
    const form = new FormData();
    form.append('File', params.file, 'photo.jpg');
    form.append('Kind', params.kind);
    if (params.caption != null) form.append('Caption', params.caption);
    if (params.latitude != null) form.append('Latitude', String(params.latitude));
    if (params.longitude != null) form.append('Longitude', String(params.longitude));

    const resp = await apiClient.post<MarinaPhotoDto>(
      `/marinas/${marinaId}/photos`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return resp.data;
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
