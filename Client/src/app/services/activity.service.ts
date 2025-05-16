import { Injectable } from "@angular/core"
import { BehaviorSubject, type Observable } from "rxjs"
import type { Activity } from "../models/activity.model"

@Injectable({
  providedIn: "root",
})
export class ActivityService {
  private mockActivities: Activity[] = [
    {
      id: "act1",
      type: "expense",
      description: "You paid ₹1500 for Dinner at Taj in Goa Trip",
      amount: 1500,
      date: new Date("2023-02-20"),
      groupId: "group1",
      groupName: "Goa Trip",
      icon: "restaurant",
    },
    {
      id: "act2",
      type: "expense",
      description: "You paid ₹25000 for Rent March in Apartment",
      amount: 25000,
      date: new Date("2023-03-01"),
      groupId: "group2",
      groupName: "Apartment",
      icon: "home",
    },
    {
      id: "act3",
      type: "expense",
      description: "Mike paid ₹800 for Movie tickets in Movie Night",
      amount: 800,
      date: new Date("2023-02-15"),
      groupId: "group3",
      groupName: "Movie Night",
      icon: "movie",
    },
    {
      id: "act4",
      type: "settlement",
      description: "You settled ₹350 with Alex",
      amount: 350,
      date: new Date("2023-03-02"),
      friendId: "friend4",
      friendName: "Alex Brown",
      icon: "payments",
    },
    {
      id: "act5",
      type: "group",
      description: "You created group Movie Night",
      date: new Date("2023-02-10"),
      groupId: "group3",
      groupName: "Movie Night",
      icon: "group_add",
    },
    {
      id: "act6",
      type: "friend",
      description: "You added Sarah as a friend",
      date: new Date("2023-02-05"),
      friendId: "friend3",
      friendName: "Sarah Williams",
      icon: "person_add",
    },
  ]

  private activitiesSubject = new BehaviorSubject<Activity[]>(this.mockActivities)
  activities$: Observable<Activity[]> = this.activitiesSubject.asObservable()

  constructor() {}

  getActivities(): Observable<Activity[]> {
    return this.activities$
  }

  addActivity(activity: Omit<Activity, "id">): void {
    const newActivity: Activity = {
      ...activity,
      id: `act${this.mockActivities.length + 1}`,
      date: new Date(),
    }

    this.mockActivities.unshift(newActivity)
    this.activitiesSubject.next([...this.mockActivities])
  }
}
