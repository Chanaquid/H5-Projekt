export namespace ChatDTO {
  export namespace LoanMessageDTO {
    // Requests
    export interface SendLoanMessageDTO {
      loanId: number;
      content: string;
    }
 
    // Responses
    export interface LoanMessageResponseDTO {
      id: number;
      loanId: number;
      senderId: string;
      senderName: string;
      senderAvatarUrl?: string;
      content: string;
      isRead: boolean;
      sentAt: string;
    }
 
    export interface LoanMessageThreadDTO {
      loanId: number;
      itemTitle: string;
      otherPartyName: string;
      otherPartyAvatarUrl?: string;
      messages: LoanMessageResponseDTO[];
    }
  }
 
  export namespace DirectMessageDTO {
    // Requests
    export interface SendDirectMessageDTO {
      recipientUsernameOrEmail: string;
      content: string;
    }
 
    // Responses
    export interface DirectMessageResponseDTO {
      id: number;
      conversationId: number;
      senderId: string;
      senderName: string;
      senderAvatarUrl?: string;
      content: string;
      isRead: boolean;
      sentAt: string;
    }
 
    export interface DirectConversationSummaryDTO {
      id: number;
      otherUserId: string;
      otherUserName: string;
      otherUserAvatarUrl?: string;
      lastMessageContent?: string;
      lastMessageAt?: string;
      unreadCount: number;
      isHidden: boolean;
      createdAt: string;
    }
 
    export interface DirectMessageThreadDTO {
      conversationId: number;
      otherUserId: string;
      otherUserName: string;
      otherUserAvatarUrl?: string;
      messages: DirectMessageResponseDTO[];
    }
  }
 
  export namespace SupportChatDTO {
    // Requests
    export interface CreateSupportThreadDTO {
      initialMessage: string;
    }
 
    export interface SendSupportMessageDTO {
      supportThreadId: number;
      content: string;
    }
 
    export interface ClaimThreadDTO {
      supportThreadId: number;
    }
 
    export interface CloseThreadDTO {
      supportThreadId: number;
      closingNote?: string;
    }
 
    // Responses
    export interface SupportMessageResponseDTO {
      id: number;
      supportThreadId: number;
      senderId: string;
      senderName: string;
      senderAvatarUrl?: string;
      isAdminSender: boolean;
      content: string;
      isRead: boolean;
      sentAt: string;
    }
 
    export interface SupportThreadSummaryDTO {
      id: number;
      userId: string;
      userName: string;
      userAvatarUrl?: string;
      status: string; // "Open" | "Claimed" | "Closed"
      claimedByAdminName?: string;
      lastMessageContent?: string;
      lastMessageAt?: string;
      unreadCount: number;
      createdAt: string;
    }
 
    export interface SupportThreadDetailDTO {
      id: number;
      userId: string;
      userName: string;
      userAvatarUrl?: string;
      status: string;
      claimedByAdminId?: string;
      claimedByAdminName?: string;
      claimedAt?: string;
      closedAt?: string;
      createdAt: string;
      messages: SupportMessageResponseDTO[];
    }
  }
 
  export namespace UserBlockDTO {
    // Requests
    export interface BlockUserDTO {
      blockedUserId: string;
    }
 
    // Responses
    export interface BlockResponseDTO {
      blockerId: string;
      blockedId: string;
      blockedUserName: string;
      blockedUserAvatarUrl?: string;
      createdAt: string;
    }
  }
}