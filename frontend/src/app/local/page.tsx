import type { Metadata } from 'next';
import DealsListingPage from '@/components/deals/DealsListingPage';

export const metadata: Metadata = { title: 'Local Deals | DealHive' };

export default function LocalPage() {
  return (
    <DealsListingPage
      title="Local Deals"
      subtitle="Save up to 80% on restaurants, spas, activities and more near you"
      breadcrumbs={[]}
      subcategories={[
        { label: 'Beauty & Spas', href: '/local/beauty-and-spas', emoji: '💆' },
        { label: 'Things To Do', href: '/local/things-to-do', emoji: '🎭' },
        { label: 'Automotive', href: '/local/automotive', emoji: '🔧' },
        { label: 'Food & Drink', href: '/local/food-and-drink', emoji: '🍽️' },
        { label: 'Health & Fitness', href: '/local/health-and-fitness', emoji: '💪' },
        { label: 'Home Services', href: '/local/home-services', emoji: '🏠' },
      ]}
    />
  );
}
