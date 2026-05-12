import Link from 'next/link';
import DealCard, { DealCardSkeleton } from '@/components/deals/DealCard';
import type { DealSummary, PaginatedResponse } from '@/types';

const API = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000/api/v1';

interface DealsListingPageProps {
  title: string;
  subtitle?: string;
  category?: string;        // category slug for API filter
  type?: string;            // deal type filter
  breadcrumbs?: { label: string; href: string }[];
  subcategories?: { label: string; href: string; emoji?: string }[];
}

async function fetchDeals(params: {
  category?: string;
  pageSize?: number;
  sort?: string;
}): Promise<DealSummary[]> {
  try {
    const qs = new URLSearchParams();
    if (params.category) qs.set('category', params.category);
    qs.set('pageSize', String(params.pageSize ?? 24));
    if (params.sort) qs.set('sort', params.sort);

    const res = await fetch(`${API}/deals?${qs}`, { next: { revalidate: 300 } });
    if (!res.ok) return [];
    const data: PaginatedResponse<DealSummary> = await res.json();
    return data.items ?? [];
  } catch {
    return [];
  }
}

export default async function DealsListingPage({
  title,
  subtitle,
  category,
  breadcrumbs = [],
  subcategories = [],
}: DealsListingPageProps) {
  const [deals, popular] = await Promise.all([
    fetchDeals({ category, sort: 'relevance' }),
    fetchDeals({ category, sort: 'popular', pageSize: 4 }),
  ]);

  return (
    <div className="bg-[#F5F5F5] min-h-screen">
      {/* Page header */}
      <div className="bg-white border-b border-[#E0E0E0]">
        <div className="max-w-[1280px] mx-auto px-4 py-4">
          {/* Breadcrumb */}
          <nav className="text-xs text-[#636366] mb-3 flex items-center gap-1 flex-wrap">
            <Link href="/" className="hover:text-[#53A318]">Home</Link>
            {breadcrumbs.map((bc) => (
              <span key={bc.href} className="flex items-center gap-1">
                <span>›</span>
                <Link href={bc.href} className="hover:text-[#53A318]">{bc.label}</Link>
              </span>
            ))}
            <span>›</span>
            <span className="text-[#1A1A1A] font-medium">{title}</span>
          </nav>

          <h1 className="text-2xl font-black text-[#1A1A1A]">{title}</h1>
          {subtitle && <p className="text-sm text-[#636366] mt-1">{subtitle}</p>}
        </div>
      </div>

      {/* Subcategory pills */}
      {subcategories.length > 0 && (
        <div className="bg-white border-b border-[#E0E0E0]">
          <div className="max-w-[1280px] mx-auto px-4 py-3 overflow-x-auto">
            <div className="flex gap-2 flex-nowrap">
              {subcategories.map((sub) => (
                <Link
                  key={sub.href}
                  href={sub.href}
                  className="flex items-center gap-1.5 bg-[#F5F5F5] hover:bg-[#EFF7E6] border border-[#E0E0E0] hover:border-[#53A318] text-[#1A1A1A] hover:text-[#53A318] text-xs font-bold px-3 py-1.5 rounded-full whitespace-nowrap transition-colors"
                >
                  {sub.label}
                </Link>
              ))}
            </div>
          </div>
        </div>
      )}

      <div className="max-w-[1280px] mx-auto px-4 py-6">
        {/* Top picks */}
        {popular.length > 0 && (
          <section className="mb-8">
            <h2 className="text-lg font-black text-[#1A1A1A] mb-4">⭐ Top Picks</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {popular.map((deal) => <DealCard key={deal.id} deal={deal} />)}
            </div>
          </section>
        )}

        {/* All deals */}
        <section>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-black text-[#1A1A1A]">
              All {title} Deals
              {deals.length > 0 && <span className="ml-2 text-sm font-normal text-[#636366]">({deals.length} results)</span>}
            </h2>
          </div>

          {deals.length === 0 ? (
            <div className="text-center py-16 bg-white rounded border border-[#E0E0E0]">
              <p className="text-[#636366] mb-2">No deals found in this category</p>
              <Link href="/local" className="text-[#53A318] font-bold hover:underline text-sm">Browse all local deals →</Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {deals.map((deal) => <DealCard key={deal.id} deal={deal} />)}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
