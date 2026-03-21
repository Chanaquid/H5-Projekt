import { CanActivateFn, Router, Routes } from '@angular/router';
import { Login } from './components/login/login';
import { Register } from './components/register/register';
import { inject } from '@angular/core';
import { AuthService } from './services/auth-service';
import { Landing } from './components/landing/landing';
import { Home } from './components/home/home';
import { Item } from './components/item/item';
import {ItemDetails } from './components/item-details/item-details';
import { PageNotFound } from './components/page-not-found/page-not-found';
import { Profile } from './components/profile/profile';
import { LoanDetails } from './components/loan-details/loan-details';
import { Loan } from './components/loan/loan';
import { AdminDashboard } from './components/admin-dashboard/admin-dashboard';
import { AdminGuard } from './services/admin-guard';
import { AdminItem } from './components/admin-item/admin-item';
import { UserProfile } from './components/user-profile/user-profile';
import { Favorites } from './components/favorites/favorites';
import { ResolutionCenter } from './components/resolution-center/resolution-center';
import { Chat } from './components/chat/chat';
import { AdminLoan } from './components/admin-loan/admin-loan';
import { AdminUser } from './components/admin-user/admin-user';
import { AdminVerification } from './components/admin-verification/admin-verification';

export const authGuard : CanActivateFn = () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    return auth.isLoggedIn() ? true : router.createUrlTree(['/']);
}

export const routes: Routes = [
    {path:'', component: Landing},
    {path:'login', component: Login},
    {path:'register', component: Register},
    {path:'home', component: Home, canActivate: [authGuard]},
    {path:'my-chats', component: Chat, canActivate: [authGuard]},
    {path:'my-items', component: Item, canActivate: [authGuard]},
    {path:'my-loans', component: Loan, canActivate: [authGuard]},
    {path:'favorites', component: Favorites, canActivate: [authGuard]},
    {path:'my-dashboard', component: Profile, canActivate: [authGuard]},
    {path:'my-resolution-center', component: ResolutionCenter, canActivate: [authGuard]},
    {path: 'items/:id', component: ItemDetails, canActivate: [authGuard]},
    {path: 'loans/:id', component: LoanDetails, canActivate: [authGuard]},
    {path: 'users/:id', component: UserProfile, canActivate: [authGuard]},
    { path: 'admin-dashboard', component: AdminDashboard, canActivate: [AdminGuard] },
    { path: 'admin-items', component: AdminItem, canActivate: [AdminGuard] },
    { path: 'admin-loans', component: AdminLoan, canActivate: [AdminGuard] },
    { path: 'admin-users', component: AdminUser, canActivate: [AdminGuard] },
    { path: 'admin-verifications', component: AdminVerification, canActivate: [AdminGuard] },

    { path: '404', component: PageNotFound },
    { path: '**', redirectTo: '/404' }
];
