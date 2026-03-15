import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth-service';
import { ItemService } from '../../services/item-service';
import { UserService } from '../../services/user-service';

@Component({
  selector: 'app-landing',
  imports: [CommonModule, RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements OnInit {

  featuredItems: any[] = [];
  stats = {
    activeItems: '...',
    members: '...',
    cities: '1000+',
  };

  constructor(private authService: AuthService,
     private router: Router,
      private itemService: ItemService,
       private userService : UserService,
      private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    // Redirect logged-in users straight to the home/dashboard
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/home']);
    }

    this.loadStats();
  }


  private loadStats() {
    this.itemService.getAll().subscribe({
      next: (items) => {
        this.stats.activeItems = items.length.toString() + '+';
        console.log(items);

        // Take the 4 most recent and map to template shape
        this.featuredItems = items.slice(0, 4).map(item => ({
          id: item.id,
          emoji: this.getCategoryEmoji(item.categoryName),
          category: item.categoryName,
          title: item.title,
          location: item.pickupAddress,
          owner: item.ownerName,
          ownerInitials: item.ownerName.split(' ').map((n: string) => n[0]).join('').toUpperCase().slice(0, 2),
          rating: item.averageRating > 0 ? `${item.averageRating.toFixed(1)} (${item.reviewCount})` : 'New',
          available: !item.isCurrentlyOnLoan,
          primaryPhotoUrl: item.primaryPhotoUrl,
        }));






        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW
      },
      error: (err) => {
        this.stats.activeItems = '2500+',
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW

      }

    });

    this.userService.getAllUsers().subscribe({
      next: (users) => {
        
        this.stats.members = users.length.toString() + '+';
        console.log(users)
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW

      },
      error: (err) => {
        
        this.stats.members = '5000+'
        this.cdr.detectChanges(); //Manually tell Angular to update the UI NOW

      }
    });
  }

  private getCategoryEmoji(categoryName: string): string {
    const map: Record<string, string> = {
      tools: '🔧', photography: '📷', bikes: '🚲', outdoors: '🏕️',
      music: '🎸', gaming: '🎮', books: '📚', travel: '🧳',
      garden: '🪴', art: '🎨', fashion: '👗', kitchen: '🍳',
      electronics: '📱', sports: '⚽', camping: '🏕️',
       others: '📦'

    };
    return map[categoryName.toLowerCase()] ?? '📦';
  }



  steps = [
    {
      number: '01',
      icon: '🔍',
      title: 'Find what you need',
      desc: 'Browse thousands of items listed by people near you. Filter by category, condition, or availability.',
    },
    {
      number: '02',
      icon: '📅',
      title: 'Request a loan',
      desc: 'Pick your dates and send a request. The owner reviews and approves within hours.',
    },
    {
      number: '03',
      icon: '🤝',
      title: 'Pick up & return',
      desc: 'Collect the item, use it, and return it on time. Your trust score grows with every smooth loan.',
    },
    {
      number: '04',
      icon: '⭐',
      title: 'Rate & repeat',
      desc: 'Leave a review and build your reputation. Higher scores unlock more items instantly.',
    },
  ];

  categories = [
    { icon: '📱', name: 'Electronics' },
    { icon: '🔧', name: 'Tools' },
    { icon: '⚽', name: 'Sports' },
    { icon: '🎸', name: 'Music' },
    { icon: '📚', name: 'Books' },
    { icon: '⛺', name: 'Camping' },
    { icon: '📷', name: 'Photography' },
    { icon: '🎮', name: 'Gaming' },
    { icon: '🌱', name: 'Gardening' },  
    { icon: '🚲', name: 'Biking' },    
    { icon: '🍳', name: 'Kitchen' },    
    { icon: '🧹', name: 'Cleaning' },   
    { icon: '👗', name: 'Fashion' },    
    { icon: '🎨', name: 'Art' },      
    { icon: '👶', name: 'Baby' },     
    { icon: '🎉', name: 'Events' },    
    { icon: '🚗', name: 'Auto' },        
    { icon: '📦', name: 'Other' },
  ];


  trustItems = [
    {
      icon: '🏅',
      title: 'Trust Score',
      desc: 'Every member earns a live score based on returns, reviews, and behaviour.',
    },
    {
      icon: '📸',
      title: 'Photo Snapshots',
      desc: 'Item condition is documented before every loan so there\'s hardly ever dispute about damage.',
    },
    {
      icon: '⚖️',
      title: 'Dispute Resolution',
      desc: 'Our admin team reviews evidence from both sides and issues fair verdicts within 72h.',
    },
    {
      icon: '✅',
      title: 'ID Verification',
      desc: 'Verified members unlock higher-value items and get a badge on their profile.',
    },
  ];
}