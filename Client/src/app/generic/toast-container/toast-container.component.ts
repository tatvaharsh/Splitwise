import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../services/toast-service.service';
import { ToastNotificationComponent } from '../toast-notification/toast-notification.component';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="toast-container">
      @for(toast of toastService.toasts; track toast){
      <app-toast-notification
        [message]="toast.message"
        [duration]="toast.duration"
      >
      </app-toast-notification>
      }
    </div>
  `,
  standalone: true,
  styleUrls: ['./toast-container.component.scss'],
  imports: [ToastNotificationComponent, CommonModule],
})
export class ToastContainerComponent {
  constructor(public toastService: ToastService) {}
}
