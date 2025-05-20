import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, type Observable, of } from 'rxjs';
import type { Group } from '../models/group.model';
import type { User } from '../models/user.model';
import { HttpClient } from '@angular/common/http';
import { environment } from '../generic/environment';
import { IResponse } from '../generic/response';



  export interface Group1 {
    id: string
    name: string
    image?: string
    members: User[]
    createdAt: Date
    lastExpense?: {
      description: string
      amount: number
      date: Date
    }
  }
  


@Injectable({
  providedIn: 'root',
})
export class GroupService {
  private apiUrl = `http://localhost:5158/api/Group/`;
  constructor(private http: HttpClient) { } 

  createGroup(formData: FormData): Observable<IResponse<null>> {
    return this.http.post<IResponse<null>>(`${this.apiUrl}create`, formData);
  }

  updateGroup(id:string,formData: FormData): Observable<IResponse<null>>{
    return this.http.put<IResponse<null>>(`${this.apiUrl}update/${id}`, formData);
  }

  deleteGroup(id: string): Observable<IResponse<null>> {
    return this.http.delete<IResponse<null>>(`${this.apiUrl}delete/${id}`);
  }

  getGroups(): Observable<
    IResponse<Group[]>
  > {
    return this.http.get<IResponse<Group[]>>(
      `${this.apiUrl}GetList`
    );
  }

  getGroupById(id: string): Observable<IResponse<Group>> {
    return this.http.get<IResponse<Group>>(
      `${this.apiUrl}get/${id}`
    );
  }
  
  private mockUsers: User[] = [
    {
      id: 'user1',
      name: 'John Doe',
      email: 'john@example.com',
      profilePic: 'assets/user1.jpg',
    },
    {
      id: 'user2',
      name: 'Jane Smith',
      email: 'jane@example.com',
      profilePic: 'assets/user2.jpg',
    },
    {
      id: 'user3',
      name: 'Mike Johnson',
      email: 'mike@example.com',
      profilePic: 'assets/user3.jpg',
    },
    {
      id: 'user4',
      name: 'Sarah Williams',
      email: 'sarah@example.com',
      profilePic: 'assets/user4.jpg',
    },
  ];

  private mockGroups: Group1[] = [
    {
      id: 'group1',
      name: 'Goa Trip',
      image: 'assets/goa.jpg',
      members: [this.mockUsers[0], this.mockUsers[1], this.mockUsers[2]],
      createdAt: new Date('2023-01-15'),
      lastExpense: {
        description: 'Dinner',
        amount: 1500,
        date: new Date('2023-02-20'),
      },
    },
    {
      id: 'group2',
      name: 'Apartment',
      image: 'assets/apartment.jpg',
      members: [this.mockUsers[0], this.mockUsers[3]],
      createdAt: new Date('2022-11-05'),
      lastExpense: {
        description: 'Rent',
        amount: 25000,
        date: new Date('2023-03-01'),
      },
    },
    {
      id: 'group3',
      name: 'Movie Night',
      image: 'assets/movie.jpg',
      members: [
        this.mockUsers[0],
        this.mockUsers[1],
        this.mockUsers[2],
        this.mockUsers[3],
      ],
      createdAt: new Date('2023-02-10'),
      lastExpense: {
        description: 'Tickets',
        amount: 800,
        date: new Date('2023-02-15'),
      },
    },
  ];

  private groupsSubject = new BehaviorSubject<Group1[]>(this.mockGroups);
  groups$: Observable<Group1[]> = this.groupsSubject.asObservable();

  getGroupss(): Observable<Group1[]> {
    return this.groups$;
  }

  // getGroupById(id: string): Observable<Group1 | undefined> {
  //   const group = this.mockGroups.find((g) => g.id === id);
  //   return of(group);
  // }

  // addGroup(group: Omit<Group, 'id' | 'createdAt'>): void {
  //   const newGroup: Group = {
  //     ...group,
  //     id: `group${this.mockGroups.length + 1}`,
  //     createdAt: new Date(),
  //   };

  //   this.mockGroups.push(newGroup);
  //   this.groupsSubject.next([...this.mockGroups]);
  // }

  // updateGroup(group: Group): void {
  //   const index = this.mockGroups.findIndex((g) => g.id === group.id);
  //   if (index !== -1) {
  //     this.mockGroups[index] = group;
  //     this.groupsSubject.next([...this.mockGroups]);
  //   }
  // }

  // addMemberToGroup(groupId: string, user: User): void {
  //   const group = this.mockGroups.find((g) => g.id === groupId);
  //   if (group && !group.members.some((m) => m.id === user.id)) {
  //     group.members.push(user);
  //     this.groupsSubject.next([...this.mockGroups]);
  //   }
  // }

  // getAvailableUsers(groupId: string): User[] {
  //   const group = this.mockGroups.find((g) => g.id === groupId);
  //   if (!group) return this.mockUsers;

  //   return this.mockUsers.filter(
  //     (user) => !group.members.some((member) => member.id === user.id)
  //   );
  // }
}
