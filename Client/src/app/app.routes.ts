import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard-component/dashboard-component.component';
import { GroupDetailComponentComponent } from './group-detail-component/group-detail-component.component';
import { GroupsComponent } from './components/groups/groups.component';
import { GroupDetailComponent } from './components/group-detail/group-detail.component';
import { FriendsComponent } from './components/friends/friends.component';
import { FriendDetailComponent } from './components/friend-detail/friend-detail.component';
import { ActivityComponent } from './components/activity/activity.component';
import { ProfileComponent } from './components/profile/profile.component';

// export const routes: Routes = [
//     { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
//     { path: 'dashboard', component: DashboardComponent },
//     { path: 'groups/:id', component: GroupDetailComponentComponent },
//     { path: '**', redirectTo: '/dashboard' }
//   ];

export const routes: Routes = [
  { path: "", redirectTo: "/groups", pathMatch: "full" },
  { path: "groups", component: GroupsComponent },
  { path: "groups/:id", component: GroupDetailComponent },
  { path: "friends", component: FriendsComponent },
  { path: "friends/:id", component: FriendDetailComponent },
  { path: "activity", component: ActivityComponent },
  { path: "profile", component: ProfileComponent },
  { path: "**", redirectTo: "/groups" },
]
