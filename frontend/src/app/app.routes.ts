import { CanActivateFn, Router, Routes } from '@angular/router';
import { Login } from './components/login/login';
import { Register } from './components/register/register';
import { inject } from '@angular/core';
import { AuthService } from './services/auth-service';
import { Landing } from './components/landing/landing';
import { Home } from './components/home/home';
import { Item } from './components/item/item';
import { ItemDetails } from './components/item-details/item-details';
import { PageNotFound } from './components/page-not-found/page-not-found';

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
    {path:'item', component: Item, canActivate: [authGuard]},
    {path: 'items/:id', component: ItemDetails, canActivate: [authGuard]},


    { path: '404', component: PageNotFound },
    { path: '**', redirectTo: '/404' }
];
