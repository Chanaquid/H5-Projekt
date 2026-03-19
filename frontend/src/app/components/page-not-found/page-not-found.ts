import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Navbar } from "../navbar/navbar";

@Component({
  selector: 'app-page-not-found',
  imports: [RouterLink, Navbar],
  templateUrl: './page-not-found.html',
  styleUrl: './page-not-found.css',
})
export class PageNotFound {}
