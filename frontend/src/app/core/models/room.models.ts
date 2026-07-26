export interface RoomSummary {
  id: string;
  name: string;
  createdByUserId: string;
  createdAt: string;
  isPrivate: boolean;
  isMember: boolean;
}

export type RoomDetails = RoomSummary;

export interface RoomMember {
  roomId: string;
  userId: string;
  username: string;
  joinedAt: string;
}

export interface CreateRoomRequest {
  name: string;
  isPrivate: boolean;
  password: string | null;
}

export interface RoomInvite {
  token: string;
}
