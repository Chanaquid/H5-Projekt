import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth-service';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {

  private hubConnection: signalR.HubConnection | null = null;

  constructor(private authService: AuthService) {}

  startConnection(): Promise<void> {
    if (this.hubConnection) {
      if (
        this.hubConnection.state === signalR.HubConnectionState.Connected ||
        this.hubConnection.state === signalR.HubConnectionState.Connecting ||
        this.hubConnection.state === signalR.HubConnectionState.Reconnecting
      ) {
        return Promise.resolve();
      }
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7183/hubs/chat', {
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    return this.hubConnection.start();
  
  }

  stopConnection(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return this.hubConnection.stop();
    }
    return Promise.resolve();
  }

  joinLoanGroup(loanId: number): Promise<void> {
    return this.hubConnection!.invoke('JoinLoanChat', loanId);
  }

  leaveLoanGroup(loanId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return this.hubConnection.invoke('LeaveLoanChat', loanId);
    }
    return Promise.resolve();
  }

  joinUserNotifications(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return this.hubConnection.invoke('JoinUserGroup');
    }
    return Promise.resolve();
  }

  leaveUserNotifications(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return this.hubConnection.invoke('LeaveUserGroup');
    }
    return Promise.resolve();
  }

  onReceiveMessage(callback: (msg: any) => void): void {
    this.hubConnection!.off('ReceiveMessage');
    this.hubConnection!.on('ReceiveMessage', callback);
  }

  offReceiveMessage(): void {
    this.hubConnection?.off('ReceiveMessage');
  }

  onMessagesRead(callback: (data: any) => void): void {
    this.hubConnection!.off('MessagesRead');
    this.hubConnection!.on('MessagesRead', callback);
  }

  offMessagesRead(): void {
    this.hubConnection?.off('MessagesRead');
  }

  get isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  onNewNotification(callback: (notification: any) => void): void {
    this.hubConnection!.off('NewNotification');
    this.hubConnection!.on('NewNotification', callback);
  }

  offNewNotification(): void {
    this.hubConnection?.off('NewNotification');
  }

  onNewMessageNotification(callback: (data: any) => void): void {
    this.hubConnection!.off('NewMessageNotification');
    this.hubConnection!.on('NewMessageNotification', callback);
  }

  offNewMessageNotification(): void {
    this.hubConnection?.off('NewMessageNotification');
  }


}
