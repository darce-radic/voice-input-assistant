// SEO Utilities for Voice Input Assistant
// Comprehensive SEO optimization functions for structured data, meta tags, and search engine optimization

import { SitemapItem, FAQItem, BreadcrumbItem, ReviewData, OfferData, StructuredDataOptions } from '../types/seo';

/**
 * Generate JSON-LD structured data for various content types
 */
export const generateStructuredData = (options: StructuredDataOptions) => {
  const baseStructure = {
    '@context': 'https://schema.org',
    '@type': options.type || 'WebApplication',
    name: options.name,
    description: options.description,
    url: options.url,
    applicationCategory: options.applicationCategory,
    operatingSystem: options.operatingSystem,
    image: options.image || `${options.url}/og-image.png`,
    screenshot: options.screenshot || `${options.url}/app-screenshot.png`,
    datePublished: options.datePublished || '2024-01-01',
    dateModified: options.dateModified || new Date().toISOString().split('T')[0],
    author: {
      '@type': 'Organization',
      name: 'Voice Input Assistant Team',
      url: options.url
    },
    publisher: {
      '@type': 'Organization',
      name: 'Voice Input Assistant',
      logo: {
        '@type': 'ImageObject',
        url: `${options.url}/logo-512x512.png`,
        width: 512,
        height: 512
      }
    }
  };

  // Add offers if provided
  if (options.offers) {
    Object.assign(baseStructure, {
      offers: {
        '@type': 'Offer',
        price: options.offers.price,
        priceCurrency: options.offers.priceCurrency,
        availability: `https://schema.org/${options.offers.availability}`,
        validFrom: options.offers.validFrom || new Date().toISOString().split('T')[0]
      }
    });
  }

  // Add aggregate rating if provided
  if (options.aggregateRating) {
    Object.assign(baseStructure, {
      aggregateRating: {
        '@type': 'AggregateRating',
        ratingValue: options.aggregateRating.ratingValue,
        ratingCount: options.aggregateRating.ratingCount,
        bestRating: options.aggregateRating.bestRating,
        worstRating: options.aggregateRating.worstRating
      }
    });
  }

  // Add features as additional properties
  if (options.features && options.features.length > 0) {
    Object.assign(baseStructure, {
      featureList: options.features,
      applicationSubCategory: 'Voice Recognition Software'
    });
  }

  return baseStructure;
};

/**
 * Generate breadcrumb structured data
 */
export const generateBreadcrumbData = (breadcrumbs: BreadcrumbItem[]) => {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: breadcrumbs.map((crumb, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: crumb.name,
      item: crumb.url
    }))
  };
};

/**
 * Generate FAQ structured data
 */
export const generateFAQData = (faqs: FAQItem[]) => {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: faqs.map(faq => ({
      '@type': 'Question',
      name: faq.question,
      acceptedAnswer: {
        '@type': 'Answer',
        text: faq.answer
      }
    }))
  };
};

/**
 * Generate Review/Testimonial structured data
 */
export const generateReviewData = (reviews: ReviewData[]) => {
  return reviews.map(review => ({
    '@context': 'https://schema.org',
    '@type': 'Review',
    author: {
      '@type': 'Person',
      name: review.authorName
    },
    datePublished: review.datePublished,
    description: review.reviewBody,
    name: review.headline,
    reviewRating: {
      '@type': 'Rating',
      bestRating: '5',
      ratingValue: review.ratingValue.toString(),
      worstRating: '1'
    }
  }));
};

/**
 * Generate Product/Service structured data
 */
export const generateProductData = (productInfo: {
  name: string;
  description: string;
  image: string;
  brand: string;
  offers: OfferData[];
  aggregateRating?: {
    ratingValue: string;
    ratingCount: string;
  };
}) => {
  const productStructure = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: productInfo.name,
    description: productInfo.description,
    image: productInfo.image,
    brand: {
      '@type': 'Brand',
      name: productInfo.brand
    },
    applicationCategory: 'BusinessApplication',
    operatingSystem: 'Web Browser, iOS, Android',
    offers: productInfo.offers.map(offer => ({
      '@type': 'Offer',
      price: offer.price,
      priceCurrency: offer.priceCurrency,
      availability: `https://schema.org/${offer.availability}`,
      name: offer.name,
      description: offer.description
    }))
  };

  if (productInfo.aggregateRating) {
    Object.assign(productStructure, {
      aggregateRating: {
        '@type': 'AggregateRating',
        ratingValue: productInfo.aggregateRating.ratingValue,
        ratingCount: productInfo.aggregateRating.ratingCount,
        bestRating: '5',
        worstRating: '1'
      }
    });
  }

  return productStructure;
};

/**
 * Generate Organization structured data
 */
export const generateOrganizationData = (orgInfo: {
  name: string;
  url: string;
  logo: string;
  description: string;
  contactPoint?: {
    telephone: string;
    contactType: string;
    email?: string;
  };
  sameAs?: string[];
}) => {
  const orgStructure = {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    name: orgInfo.name,
    url: orgInfo.url,
    logo: orgInfo.logo,
    description: orgInfo.description
  };

  if (orgInfo.contactPoint) {
    Object.assign(orgStructure, {
      contactPoint: {
        '@type': 'ContactPoint',
        telephone: orgInfo.contactPoint.telephone,
        contactType: orgInfo.contactPoint.contactType,
        email: orgInfo.contactPoint.email
      }
    });
  }

  if (orgInfo.sameAs && orgInfo.sameAs.length > 0) {
    Object.assign(orgStructure, { sameAs: orgInfo.sameAs });
  }

  return orgStructure;
};

/**
 * Generate meta tags object for React Helmet
 */
export const generateMetaTags = (pageInfo: {
  title: string;
  description: string;
  keywords?: string;
  canonicalUrl: string;
  ogImage?: string;
  twitterImage?: string;
  articleAuthor?: string;
  publishedTime?: string;
  modifiedTime?: string;
}) => {
  const metaTags = [
    { name: 'title', content: pageInfo.title },
    { name: 'description', content: pageInfo.description },
    { name: 'robots', content: 'index, follow, max-snippet:-1, max-image-preview:large, max-video-preview:-1' },
    
    // Open Graph
    { property: 'og:title', content: pageInfo.title },
    { property: 'og:description', content: pageInfo.description },
    { property: 'og:url', content: pageInfo.canonicalUrl },
    { property: 'og:type', content: 'website' },
    { property: 'og:site_name', content: 'Voice Input Assistant' },
    
    // Twitter
    { name: 'twitter:card', content: 'summary_large_image' },
    { name: 'twitter:title', content: pageInfo.title },
    { name: 'twitter:description', content: pageInfo.description },
    { name: 'twitter:site', content: '@VoiceInputAI' },
    { name: 'twitter:creator', content: '@VoiceInputAI' }
  ];

  if (pageInfo.keywords) {
    metaTags.push({ name: 'keywords', content: pageInfo.keywords });
  }

  if (pageInfo.ogImage) {
    metaTags.push({ property: 'og:image', content: pageInfo.ogImage });
    metaTags.push({ property: 'og:image:width', content: '1200' });
    metaTags.push({ property: 'og:image:height', content: '630' });
  }

  if (pageInfo.twitterImage) {
    metaTags.push({ name: 'twitter:image', content: pageInfo.twitterImage });
  }

  if (pageInfo.articleAuthor) {
    metaTags.push({ name: 'author', content: pageInfo.articleAuthor });
    metaTags.push({ property: 'article:author', content: pageInfo.articleAuthor });
  }

  if (pageInfo.publishedTime) {
    metaTags.push({ property: 'article:published_time', content: pageInfo.publishedTime });
  }

  if (pageInfo.modifiedTime) {
    metaTags.push({ property: 'article:modified_time', content: pageInfo.modifiedTime });
  }

  return metaTags;
};

/**
 * Generate sitemap data
 */
export const generateSitemapData = (baseUrl: string): SitemapItem[] => {
  return [
    {
      url: baseUrl,
      changefreq: 'daily',
      priority: '1.0',
      lastmod: new Date().toISOString().split('T')[0]
    },
    {
      url: `${baseUrl}/features`,
      changefreq: 'weekly',
      priority: '0.8',
      lastmod: new Date().toISOString().split('T')[0]
    },
    {
      url: `${baseUrl}/pricing`,
      changefreq: 'weekly',
      priority: '0.8',
      lastmod: new Date().toISOString().split('T')[0]
    },
    {
      url: `${baseUrl}/about`,
      changefreq: 'monthly',
      priority: '0.7',
      lastmod: new Date().toISOString().split('T')[0]
    },
    {
      url: `${baseUrl}/contact`,
      changefreq: 'monthly',
      priority: '0.6',
      lastmod: new Date().toISOString().split('T')[0]
    },
    {
      url: `${baseUrl}/privacy`,
      changefreq: 'monthly',
      priority: '0.5',
      lastmod: new Date().toISOString().split('T')[0]
    },
    {
      url: `${baseUrl}/terms`,
      changefreq: 'monthly',
      priority: '0.5',
      lastmod: new Date().toISOString().split('T')[0]
    },
    {
      url: `${baseUrl}/app`,
      changefreq: 'daily',
      priority: '0.9',
      lastmod: new Date().toISOString().split('T')[0]
    }
  ];
};

/**
 * Generate robots.txt content
 */
export const generateRobotsTxt = (baseUrl: string): string => {
  return `User-agent: *
Allow: /

# Sitemap location
Sitemap: ${baseUrl}/sitemap.xml

# Crawl delay for polite crawling
Crawl-delay: 1

# Disallow sensitive areas
Disallow: /admin/
Disallow: /api/
Disallow: /private/
Disallow: /*.json$
Disallow: /*?*

# Allow important resources
Allow: /images/
Allow: /static/
Allow: /assets/
Allow: /*.css
Allow: /*.js
Allow: /*.png
Allow: /*.jpg
Allow: /*.svg
Allow: /*.webp

# Special instructions for different bots
User-agent: Googlebot
Allow: /

User-agent: Bingbot
Allow: /

User-agent: facebookexternalhit
Allow: /

User-agent: Twitterbot
Allow: /`;
};

/**
 * Validate structured data
 */
export const validateStructuredData = (data: any): boolean => {
  try {
    // Basic validation
    if (!data['@context'] || !data['@type']) {
      console.warn('Structured data missing @context or @type');
      return false;
    }

    // Validate required fields based on type
    if (data['@type'] === 'WebApplication') {
      const required = ['name', 'description', 'url'];
      for (const field of required) {
        if (!data[field]) {
          console.warn(`Structured data missing required field: ${field}`);
          return false;
        }
      }
    }

    return true;
  } catch (error) {
    console.error('Error validating structured data:', error);
    return false;
  }
};

/**
 * Generate hreflang tags for internationalization
 */
export const generateHreflangTags = (baseUrl: string, currentLanguage: string = 'en') => {
  const languages = {
    'en': 'English',
    'es': 'Español',
    'fr': 'Français',
    'de': 'Deutsch',
    'it': 'Italiano',
    'pt': 'Português',
    'ja': '日本語',
    'ko': '한국어',
    'zh': '中文'
  };

  return Object.keys(languages).map(lang => ({
    rel: 'alternate',
    hrefLang: lang,
    href: lang === 'en' ? baseUrl : `${baseUrl}/${lang}`
  })).concat([
    {
      rel: 'alternate',
      hrefLang: 'x-default',
      href: baseUrl
    }
  ]);
};

/**
 * Extract keywords from content for meta tags
 */
export const extractKeywords = (content: string, maxKeywords: number = 20): string => {
  // Simple keyword extraction - in production, consider using NLP libraries
  const commonWords = ['the', 'is', 'at', 'which', 'on', 'a', 'an', 'and', 'or', 'but', 'in', 'with', 'to', 'for', 'of', 'as', 'by', 'that', 'this', 'it', 'from', 'they', 'we', 'you', 'have', 'had', 'has', 'can', 'could', 'will', 'would'];
  
  const words = content
    .toLowerCase()
    .replace(/[^\w\s]/g, '')
    .split(/\s+/)
    .filter(word => word.length > 3 && !commonWords.includes(word))
    .reduce((acc: { [key: string]: number }, word) => {
      acc[word] = (acc[word] || 0) + 1;
      return acc;
    }, {});

  return Object.entries(words)
    .sort(([,a], [,b]) => (b as number) - (a as number))
    .slice(0, maxKeywords)
    .map(([word]) => word)
    .join(', ');
};

export default {
  generateStructuredData,
  generateBreadcrumbData,
  generateFAQData,
  generateReviewData,
  generateProductData,
  generateOrganizationData,
  generateMetaTags,
  generateSitemapData,
  generateRobotsTxt,
  validateStructuredData,
  generateHreflangTags,
  extractKeywords
};