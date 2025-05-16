export interface Group {
  id: string
  groupname: string
  autoLogo?: string
  totalMember: Number
  members?: Member[];
}

export interface Member {
    id: string;
    name: string;
}