export interface Activity {
    id: string
    type: "expense" | "settlement" | "group" | "friend"
    description: string
    amount?: number
    date: Date
    groupId?: string
    groupName?: string
    friendId?: string
    friendName?: string
    icon?: string
  }
  