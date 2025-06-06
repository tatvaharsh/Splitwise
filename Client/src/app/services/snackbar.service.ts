import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SnackbarConfig } from '../generic/snackbar-config.const';
import { SnackbarComponent } from '../generic/snackbar/snackbar.component';

@Injectable({
  providedIn: 'root',
})
export class SnackbarService {
  private statusConfigs: {
    [key: string]: { panelClass: string; icon: string };
  } = {
      success: { panelClass: 'bg-success', icon: 'done' },
      error: { panelClass: 'bg-error', icon: 'report-problem' },
      warning: { panelClass: 'bg-warning', icon: 'warning_amber' },
      info: { panelClass: 'bg-info', icon: 'info' },
    };

  constructor(private snackbar: MatSnackBar) { }

  show(config: SnackbarConfig) {
    const {
      status,
      message,
      action,
      duration,
      horizontalPosition,
      verticalPosition,
    } = config;
    config._snackbar = this.snackbar;
    const statusConfig = this.statusConfigs[status ?? 'success'] || {};
    const { panelClass, icon } = statusConfig;

    this.snackbar.openFromComponent(SnackbarComponent, {
      data: config,
      duration,
      horizontalPosition,
      verticalPosition,
      panelClass,
    });
  }

  
  showError(message: string): void {
    this.show(
      new SnackbarConfig({
        message: message,
        status: 'error',
        horizontalPosition: 'right',
      })
    );
  }

  showSuccess(message: string): void {
    this.show(
      new SnackbarConfig({
        message: message,
        status: 'success',
        horizontalPosition: 'right',
      })
    );
  }
}
