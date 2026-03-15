import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-item-details',
  imports: [RouterLink],
  templateUrl: './item-details.html',
  styleUrl: './item-details.css',
})


export class ItemDetails implements OnInit {
  itemId: string | null = null;

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    // This reads the ":id" part of the URL (e.g., /items/9)
    this.itemId = this.route.snapshot.paramMap.get('id');
  }
}