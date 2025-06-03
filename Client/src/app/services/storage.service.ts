import { Injectable } from '@angular/core';
import { IJwtPayload } from '../models/auth.model';
import { GlobalConstant } from '../generic/global-const';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root',
})
export class LocalStorageService {
  private isBrowser: boolean;

  constructor() {
    this.isBrowser = typeof window !== 'undefined';
  }

  get(key: string) {
    if (!this.isBrowser) return null;

    try {
      const item = localStorage.getItem(key);
      return item ? JSON.parse(item) : null;
    } catch {
      return null;
    }
  }

  set(key: string, value: any): boolean {
    if (!this.isBrowser) return false;

    try {
      localStorage.setItem(key, JSON.stringify(value));
      return true;
    } catch {
      return false;
    }
  }

  has(key: string): boolean {
    if (!this.isBrowser) return false;
    return !!localStorage.getItem(key);
  }

  remove(key: string) {
    if (this.isBrowser) {
      localStorage.removeItem(key);
    }
  }

  clear() {
    if (this.isBrowser) {
      localStorage.clear();
    }
  }

  getDecodedToken(): IJwtPayload | null {
    const token = this.get(GlobalConstant.ACCESS_TOKEN);
    if (!token) return null;

    try {
      return jwtDecode<IJwtPayload>(token);
    } catch {
      return null;
    }
  }
}