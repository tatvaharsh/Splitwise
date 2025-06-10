import { Routes } from '@angular/router';
import { GroupsComponent } from './components/groups/groups.component';
import { GroupDetailComponent } from './components/group-detail/group-detail.component';
import { FriendsComponent } from './components/friends/friends.component';
import { FriendDetailComponent } from './components/friend-detail/friend-detail.component';
import { ActivityComponent } from './components/activity/activity.component';
import { ProfileComponent } from './components/profile/profile.component';
import {LoginComponent} from './components/login/login.component';
import { SignupComponent } from './components/sign-up/sign-up.component';
import { MainLayoutComponent } from './generic/main-layout/main-layout.component';
import { authGuard } from './generic/guard/auth.guard';
import { SettlementTransparencyComponent } from './components/settlement-transparency/settlement-transparency.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },

  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard], 
    children: [
      { path: 'groups', component: GroupsComponent },
      { path: 'groups/:id', component: GroupDetailComponent },
      { path: 'friends', component: FriendsComponent },
      { path: 'friends/:id', component: FriendDetailComponent },
      { path: 'activity', component: ActivityComponent },
      { path: 'profile', component: ProfileComponent },
      {
        path:'transparency/:id', component: SettlementTransparencyComponent
      }
    ]
  },

  { path: '**', redirectTo: 'login' }
];
