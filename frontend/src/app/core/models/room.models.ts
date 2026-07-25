export interface RoomSummary {
  id: string;
  name: string;
  createdByUserId: string;
  createdAt: string;
}

export interface RoomDetails extends RoomSummary {
  isMember: boolean;
}

export interface RoomMember {
  roomId: string;
  userId: string;
  username: string;
  joinedAt: string;
}

export interface CreateRoomRequest {
  name: string;
}
