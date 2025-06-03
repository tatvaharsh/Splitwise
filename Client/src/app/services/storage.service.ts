import { Injectable } from '@angular/core';
import { IJwtPayload } from '../models/auth.model';
import { GlobalConstant } from '../generic/global-const';
import {jwtDecode} from 'jwt-decode';

@Injectable({
  providedIn: 'root',
})
export class LocalStorageService {

  get(key: string) {
    return JSON.parse(localStorage.getItem(key) || '{}') || {};
  }

  set(key: string, value: any): boolean {
    localStorage.setItem(key, JSON.stringify(value));

    return true;
  }

  has(key: string): boolean {
    return !!localStorage.getItem(key);
  }

  remove(key: string) {
    localStorage.removeItem(key);
  }

  clear() {
    localStorage.clear();
  }

  getDecodedToken(): IJwtPayload | null {
    const token = this.get( GlobalConstant.ACCESS_TOKEN);
    if (!token) return null;
    return jwtDecode<IJwtPayload>(token);
  }
}
