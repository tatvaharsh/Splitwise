import { Component, type OnInit } from "@angular/core"
import type { Observable } from "rxjs"
import type { Activities, Activity } from "../../models/activity.model"
import { ActivityService } from "../../services/activity.service"
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from "@angular/common";
@Component({
  selector: "app-activity",
  templateUrl: "./activity.component.html",
  standalone: true,
  imports:[MatCardModule, MatIconModule, CommonModule],
  styleUrls: ["./activity.component.scss"],
})
export class ActivityComponent implements OnInit {
  activities : Activities[] = []

  constructor(private activityService: ActivityService) {
  }

  ngOnInit(): void {
    this.activityService.getActivities().subscribe((res) => {
      this.activities = res.content
    })
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    })
  }

  getActivityIcon(activity: Activity): string {
    return activity.icon || this.getDefaultIcon(activity.type)
  }

  private getDefaultIcon(type: string): string {
    switch (type) {
      case "expense":
        return "receipt_long"
      case "settlement":
        return "payments"
      case "group":
        return "group"
      case "friend":
        return "person"
      default:
        return "history"
    }
  }
}
