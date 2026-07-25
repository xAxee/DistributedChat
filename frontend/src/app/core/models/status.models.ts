export interface ApplicationStatusResponse {
  instanceId: string;
  activeConnections: number;
  connectedUsers: number;
  uptimeSeconds: number;
  startedAt: string;
  applicationVersion: string;
}
