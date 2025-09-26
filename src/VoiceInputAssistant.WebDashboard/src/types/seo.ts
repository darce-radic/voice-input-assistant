// SEO Type Definitions for Voice Input Assistant
// TypeScript interfaces and types for SEO-related functionality

export interface SitemapItem {
  url: string;
  changefreq: 'always' | 'hourly' | 'daily' | 'weekly' | 'monthly' | 'yearly' | 'never';
  priority: string;
  lastmod: string;
  alternateUrls?: {
    lang: string;
    url: string;
  }[];
}

export interface FAQItem {
  question: string;
  answer: string;
  category?: string;
}

export interface BreadcrumbItem {
  name: string;
  url: string;
  position?: number;
}

export interface ReviewData {
  authorName: string;
  datePublished: string;
  reviewBody: string;
  headline: string;
  ratingValue: number;
  authorImage?: string;
  authorTitle?: string;
  verified?: boolean;
}

export interface OfferData {
  price: string;
  priceCurrency: string;
  availability: 'InStock' | 'OutOfStock' | 'PreOrder' | 'BackOrder' | 'Discontinued';
  name: string;
  description: string;
  validFrom?: string;
  validThrough?: string;
  url?: string;
  category?: string;
}

export interface AggregateRating {
  ratingValue: string;
  ratingCount: string;
  bestRating: string;
  worstRating: string;
}

export interface StructuredDataOptions {
  type?: 'WebApplication' | 'SoftwareApplication' | 'Product' | 'Service' | 'Organization' | 'WebSite';
  name: string;
  description: string;
  url: string;
  applicationCategory?: string;
  operatingSystem?: string;
  image?: string;
  screenshot?: string;
  datePublished?: string;
  dateModified?: string;
  offers?: {
    price: string;
    priceCurrency: string;
    availability: string;
    validFrom?: string;
    validThrough?: string;
  };
  aggregateRating?: AggregateRating;
  features?: string[];
  author?: {
    name: string;
    url: string;
    type?: 'Organization' | 'Person';
  };
  publisher?: {
    name: string;
    logo: string;
    url: string;
  };
}

export interface MetaTagsOptions {
  title: string;
  description: string;
  keywords?: string;
  canonicalUrl: string;
  ogImage?: string;
  ogImageWidth?: string;
  ogImageHeight?: string;
  ogType?: string;
  twitterImage?: string;
  twitterCard?: 'summary' | 'summary_large_image' | 'app' | 'player';
  articleAuthor?: string;
  publishedTime?: string;
  modifiedTime?: string;
  locale?: string;
  siteName?: string;
  themeColor?: string;
}

export interface SocialMediaLinks {
  facebook?: string;
  twitter?: string;
  linkedin?: string;
  instagram?: string;
  youtube?: string;
  github?: string;
  discord?: string;
}

export interface OrganizationData {
  name: string;
  url: string;
  logo: string;
  description: string;
  foundingDate?: string;
  email?: string;
  telephone?: string;
  address?: {
    streetAddress?: string;
    addressLocality?: string;
    addressRegion?: string;
    postalCode?: string;
    addressCountry?: string;
  };
  contactPoint?: {
    telephone: string;
    contactType: string;
    email?: string;
    availableLanguage?: string[];
    hoursAvailable?: string;
  };
  sameAs?: string[];
  numberOfEmployees?: string;
  foundingLocation?: string;
}

export interface ProductData {
  name: string;
  description: string;
  image: string;
  brand: string;
  category: string;
  sku?: string;
  gtin?: string;
  mpn?: string;
  model?: string;
  offers: OfferData[];
  aggregateRating?: AggregateRating;
  review?: ReviewData[];
  features?: string[];
  operatingSystem?: string[];
  applicationCategory?: string;
  softwareVersion?: string;
  fileSize?: string;
  installUrl?: string;
  screenshot?: string[];
  video?: string[];
}

export interface LocalBusinessData {
  name: string;
  description: string;
  image: string;
  address: {
    streetAddress: string;
    addressLocality: string;
    addressRegion: string;
    postalCode: string;
    addressCountry: string;
  };
  geo: {
    latitude: number;
    longitude: number;
  };
  telephone: string;
  url: string;
  openingHours: string[];
  priceRange?: string;
  servesCuisine?: string[];
  acceptsReservations?: boolean;
}

export interface VideoData {
  name: string;
  description: string;
  thumbnailUrl: string;
  uploadDate: string;
  contentUrl?: string;
  embedUrl?: string;
  duration?: string;
  interactionCount?: string;
  publisher?: {
    name: string;
    logo: string;
  };
}

export interface ArticleData {
  headline: string;
  description: string;
  image: string;
  datePublished: string;
  dateModified?: string;
  author: {
    name: string;
    url?: string;
  };
  publisher: {
    name: string;
    logo: string;
  };
  mainEntityOfPage?: string;
  articleSection?: string;
  wordCount?: number;
}

export interface WebPageData {
  name: string;
  description: string;
  url: string;
  image?: string;
  lastReviewed?: string;
  primaryImageOfPage?: string;
  mainEntity?: any;
  breadcrumb?: BreadcrumbItem[];
  speakable?: {
    cssSelector: string[];
    xpath?: string[];
  };
}

export interface SearchResultsData {
  searchTerms: string;
  numberOfItems: number;
  itemListElement: Array<{
    position: number;
    url: string;
    name: string;
    description?: string;
    image?: string;
  }>;
}

// Analytics and Tracking Types

export interface AnalyticsEvent {
  action: string;
  category: string;
  label?: string;
  value?: number;
  customParameters?: Record<string, any>;
}

export interface ConversionEvent {
  eventName: string;
  value?: number;
  currency?: string;
  items?: Array<{
    item_id: string;
    item_name: string;
    category: string;
    price: number;
    quantity: number;
  }>;
  customParameters?: Record<string, any>;
}

export interface PageViewData {
  page_title: string;
  page_location: string;
  page_referrer?: string;
  content_group1?: string;
  content_group2?: string;
  custom_map?: Record<string, any>;
}

export interface UserProperties {
  user_id?: string;
  user_properties?: Record<string, any>;
  custom_parameters?: Record<string, any>;
}

// Performance and Core Web Vitals Types

export interface WebVital {
  name: 'FCP' | 'LCP' | 'FID' | 'CLS' | 'TTFB' | 'INP';
  value: number;
  rating: 'good' | 'needs-improvement' | 'poor';
  delta?: number;
  id: string;
  navigationType?: 'navigate' | 'reload' | 'back_forward' | 'back_forward_cache';
}

export interface PerformanceMetrics {
  loadTime: number;
  domContentLoaded: number;
  timeToInteractive: number;
  firstContentfulPaint: number;
  largestContentfulPaint: number;
  cumulativeLayoutShift: number;
  firstInputDelay: number;
  totalBlockingTime: number;
  speedIndex: number;
  resourceLoadTimes: Record<string, number>;
  networkInformation?: {
    effectiveType: string;
    downlink: number;
    rtt: number;
    saveData: boolean;
  };
}

// Content and Marketing Types

export interface TestimonialData {
  id: string;
  name: string;
  title: string;
  company: string;
  image: string;
  rating: number;
  testimonial: string;
  featured: boolean;
  dateSubmitted: string;
  verified: boolean;
  location?: string;
  useCase?: string;
  industry?: string;
}

export interface PricingPlan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  interval: 'month' | 'year';
  features: string[];
  limitations?: string[];
  popular?: boolean;
  ctaText: string;
  ctaUrl: string;
  trialDays?: number;
  setupFee?: number;
  stripePriceId?: string;
}

export interface FeatureData {
  id: string;
  title: string;
  description: string;
  icon: string;
  category: string;
  benefits: string[];
  technicalDetails?: string;
  availableIn?: string[];
  comingSoon?: boolean;
  beta?: boolean;
}

export interface UseCaseData {
  id: string;
  title: string;
  description: string;
  industry: string;
  image: string;
  benefits: string[];
  metrics?: {
    efficiency?: string;
    accuracy?: string;
    timeSaved?: string;
    costSaved?: string;
  };
  testimonial?: {
    quote: string;
    author: string;
    company: string;
  };
}

export interface CompetitorAnalysis {
  competitor: string;
  features: Record<string, boolean | string>;
  pricing: {
    startingPrice: number;
    currency: string;
    interval: string;
  };
  pros: string[];
  cons: string[];
  marketPosition: 'leader' | 'challenger' | 'follower' | 'niche';
}

export interface MarketingCampaign {
  id: string;
  name: string;
  type: 'social' | 'search' | 'display' | 'email' | 'content' | 'influencer';
  status: 'draft' | 'active' | 'paused' | 'completed';
  startDate: string;
  endDate?: string;
  budget?: number;
  targetAudience: {
    demographics: Record<string, any>;
    interests: string[];
    behaviors: string[];
  };
  content: {
    headlines: string[];
    descriptions: string[];
    images: string[];
    callToAction: string[];
  };
  tracking: {
    utmSource: string;
    utmMedium: string;
    utmCampaign: string;
    utmTerm?: string;
    utmContent?: string;
  };
}

// Export all types as a namespace for convenient importing
// This namespace was causing circular dependency errors and has been removed.
// Types should be imported directly from this module.