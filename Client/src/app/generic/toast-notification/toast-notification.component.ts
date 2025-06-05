import { CommonModule } from '@angular/common';
import { Component, input, Input } from '@angular/core';

@Component({
  selector: 'app-toast-notification',
  standalone:true,
  imports: [CommonModule],
  templateUrl: './toast-notification.component.html',
  styleUrl: './toast-notification.component.scss',
})
export class ToastNotificationComponent {
  message = input<string>('');
  duration = input<number>(4000);
  visible = true;

  ngOnInit(): void {
    setTimeout(() => {
      this.visible = false;
    }, this.duration());
  }
}
