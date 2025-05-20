import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface DeleteDialogState {
  isVisible: boolean;
  title: string;
  message: string;
  itemName: string;
}

@Injectable({
  providedIn: 'root'
})
export class DeleteConfirmationService {
  private initialState: DeleteDialogState = {
    isVisible: false,
    title: 'Confirm Delete',
    message: 'Are you sure you want to delete this item?',
    itemName: ''
  };

  private dialogState = new BehaviorSubject<DeleteDialogState>(this.initialState);
  
  dialogState$: Observable<DeleteDialogState> = this.dialogState.asObservable();

  open(options: Partial<DeleteDialogState> = {}): void {
    this.dialogState.next({
      ...this.initialState,
      ...options,
      isVisible: true
    });
  }

  close(): void {
    this.dialogState.next({
      ...this.dialogState.value,
      isVisible: false
    });
  }
}