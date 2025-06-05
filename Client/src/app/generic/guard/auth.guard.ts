import { inject } from '@angular/core';
import {
  ActivatedRoute,
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
} from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { GlobalConstant } from '../global-const';

export const authGuard = (
  route: ActivatedRouteSnapshot,
  state?: RouterStateSnapshot
) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.check()) {
    router.navigate(['/login']);
    return false;
  }
  return true;
};