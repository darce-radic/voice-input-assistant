import React from 'react';
import { Helmet } from 'react-helmet-async';
import HeroSection from './sections/HeroSection';
// import FeaturesSection from './sections/FeaturesSection';
// import BenefitsSection from './sections/BenefitsSection';
// import UseCasesSection from './sections/UseCasesSection';
// import TestimonialsSection from './sections/TestimonialsSection';
// import PricingSection from './sections/PricingSection';
// import FAQSection from './sections/FAQSection';
// import CTASection from './sections/CTASection';
// import ContactSection from './sections/ContactSection';
import { generateStructuredData, generateBreadcrumbData } from '../../utils/seoUtils';

interface LandingPageProps {
  className?: string;
}

const LandingPage: React.FC<LandingPageProps> = ({ className }) => {
  const pageTitle = 'Voice Input Assistant - Advanced AI Voice Transcription & Processing';
  const pageDescription = 'Transform your voice into text with our cutting-edge AI-powered voice input assistant. Real-time transcription, emotion detection, and advanced audio processing for professionals and businesses.';
  const canonicalUrl = 'https://voiceinputassistant.com';
  
  const structuredData = generateStructuredData({
    type: 'WebApplication',
    name: 'Voice Input Assistant',
    description: pageDescription,
    url: canonicalUrl,
    applicationCategory: 'BusinessApplication',
    operatingSystem: 'Web Browser, iOS, Android',
    offers: {
      price: '29.99',
      priceCurrency: 'USD',
      availability: 'InStock'
    },
    aggregateRating: {
      ratingValue: '4.8',
      ratingCount: '247',
      bestRating: '5',
      worstRating: '1'
    },
    features: [
      'Real-time voice transcription',
      'AI-powered emotion detection',
      'Multi-language support',
      'Offline functionality',
      'Privacy-focused processing'
    ]
  });

  const breadcrumbData = generateBreadcrumbData([
    { name: 'Home', url: canonicalUrl }
  ]);

  return (
    <>
      <Helmet>
        {/* Primary Meta Tags */}
        <title>{pageTitle}</title>
        <meta name="title" content={pageTitle} />
        <meta name="description" content={pageDescription} />
        <meta name="keywords" content="voice transcription, AI voice assistant, speech to text, voice input, audio processing, real-time transcription, voice recognition, AI transcription, business voice tools" />
        <meta name="author" content="Voice Input Assistant Team" />
        <meta name="robots" content="index, follow, max-snippet:-1, max-image-preview:large, max-video-preview:-1" />
        <link rel="canonical" href={canonicalUrl} />
        
        {/* Open Graph / Facebook */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Voice Input Assistant" />
        <meta property="og:title" content={pageTitle} />
        <meta property="og:description" content={pageDescription} />
        <meta property="og:image" content={`${canonicalUrl}/og-image.png`} />
        <meta property="og:image:width" content="1200" />
        <meta property="og:image:height" content="630" />
        <meta property="og:url" content={canonicalUrl} />
        <meta property="og:locale" content="en_US" />
        
        {/* Twitter */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:site" content="@VoiceInputAI" />
        <meta name="twitter:creator" content="@VoiceInputAI" />
        <meta name="twitter:title" content={pageTitle} />
        <meta name="twitter:description" content={pageDescription} />
        <meta name="twitter:image" content={`${canonicalUrl}/twitter-image.png`} />
        
        {/* Additional SEO Meta Tags */}
        <meta name="theme-color" content="#667eea" />
        <meta name="msapplication-TileColor" content="#667eea" />
        <meta name="application-name" content="Voice Input Assistant" />
        <meta name="apple-mobile-web-app-title" content="Voice Input Assistant" />
        <meta name="apple-mobile-web-app-capable" content="yes" />
        <meta name="apple-mobile-web-app-status-bar-style" content="default" />
        <meta name="mobile-web-app-capable" content="yes" />
        
        {/* Preconnect to external resources */}
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link rel="preconnect" href="https://www.google-analytics.com" />
        
        {/* Structured Data */}
        <script type="application/ld+json">
          {JSON.stringify(structuredData)}
        </script>
        <script type="application/ld+json">
          {JSON.stringify(breadcrumbData)}
        </script>
        
        {/* Alternate languages */}
        <link rel="alternate" hrefLang="en" href={canonicalUrl} />
        <link rel="alternate" hrefLang="es" href={`${canonicalUrl}/es`} />
        <link rel="alternate" hrefLang="fr" href={`${canonicalUrl}/fr`} />
        <link rel="alternate" hrefLang="de" href={`${canonicalUrl}/de`} />
        <link rel="alternate" hrefLang="x-default" href={canonicalUrl} />
      </Helmet>

      <main className={`landing-page ${className || ''}`} role="main">
        {/* Hero Section - Above the fold content */}
        <HeroSection />
        
        {/* Features Overview */}
        {/* <FeaturesSection /> */}
        
        {/* Benefits Section */}
        {/* <BenefitsSection /> */}
        
        {/* Use Cases */}
        {/* <UseCasesSection /> */}
        
        {/* Social Proof */}
        {/* <TestimonialsSection /> */}
        
        {/* Pricing */}
        {/* <PricingSection /> */}
        
        {/* FAQ */}
        {/* <FAQSection /> */}
        
        {/* Call to Action */}
        {/* <CTASection /> */}
        
        {/* Contact */}
        {/* <ContactSection /> */}
      </main>
    </>
  );
};

export default LandingPage;