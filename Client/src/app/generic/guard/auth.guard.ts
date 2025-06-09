import { inject } from '@angular/core';
import {
  ActivatedRoute,
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
} from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { GlobalConstant } from '../global-const';
import { LocalStorageService } from '../../services/storage.service';

export const authGuard = (
  route: ActivatedRouteSnapshot,
  state?: RouterStateSnapshot
) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const storage = inject(LocalStorageService);
  const decoded = storage.getDecodedToken();

  const now = Math.floor(Date.now() / 1000); // current time in seconds
  const isExpired = decoded && Number(decoded.exp) < now;

  if (!auth.check() || isExpired) {
    storage.remove(GlobalConstant.ACCESS_TOKEN);
    router.navigate(['/login']);
    return false;
  }

  return true;
};