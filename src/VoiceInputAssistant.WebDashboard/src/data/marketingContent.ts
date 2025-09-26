// Marketing Content for Voice Input Assistant
// Comprehensive marketing copy, features, benefits, testimonials, and pricing data

import { TestimonialData, PricingPlan, FeatureData, UseCaseData } from '../types/seo';

// Hero Section Content
export const heroContent = {
  badge: "🚀 Now with Advanced AI Processing",
  title: {
    main: "Transform Your Voice Into",
    highlight: "Intelligent Text"
  },
  subtitle: "Experience the future of voice input with",
  rotatingFeatures: [
    "Real-time Voice Transcription",
    "AI-Powered Emotion Detection", 
    "Multi-Language Support",
    "Privacy-First Processing",
    "Offline Functionality"
  ],
  description: "Our cutting-edge AI-powered voice assistant delivers real-time transcription, emotion detection, and advanced audio processing. Perfect for professionals, content creators, and businesses who value accuracy, privacy, and efficiency.",
  benefits: [
    { icon: "⚡", text: "99.5% Accuracy" },
    { icon: "🔒", text: "Privacy First" },
    { icon: "🌐", text: "Works Offline" },
    { icon: "📱", text: "Any Device" }
  ],
  cta: {
    primary: {
      text: "Get Started Free",
      link: "/app"
    },
    secondary: {
      text: "Watch Demo",
      action: "demo"
    }
  },
  socialProof: {
    stats: [
      { number: "10K+", label: "Active Users" },
      { number: "99.5%", label: "Accuracy Rate" },
      { number: "24/7", label: "Availability" }
    ],
    rating: {
      stars: 5,
      score: "4.8/5",
      reviews: "500+ professionals"
    }
  }
};

// Features Content
export const featuresContent: FeatureData[] = [
  {
    id: "real-time-transcription",
    title: "Real-Time Voice Transcription",
    description: "Convert speech to text instantly with industry-leading accuracy and lightning-fast processing.",
    icon: "🎙️",
    category: "Core",
    benefits: [
      "99.5% accuracy rate",
      "Real-time processing",
      "Multiple audio formats supported",
      "Noise cancellation",
      "Voice activity detection"
    ],
    technicalDetails: "Advanced deep learning models with WebRTC integration for optimal audio processing."
  },
  {
    id: "ai-emotion-detection",
    title: "AI-Powered Emotion Detection",
    description: "Understand the emotional context of speech with advanced sentiment analysis and emotion recognition.",
    icon: "🧠",
    category: "AI/ML",
    benefits: [
      "Real-time emotion analysis",
      "Sentiment scoring",
      "Confidence ratings",
      "Contextual insights",
      "Mood tracking over time"
    ],
    technicalDetails: "State-of-the-art neural networks trained on diverse emotional speech patterns."
  },
  {
    id: "multi-language-support",
    title: "Multi-Language Support",
    description: "Transcribe and process speech in 50+ languages with automatic language detection.",
    icon: "🌍",
    category: "Localization",
    benefits: [
      "50+ languages supported",
      "Automatic language detection",
      "Accent recognition",
      "Regional dialect support",
      "Real-time translation"
    ],
    technicalDetails: "Comprehensive language models covering major world languages and dialects."
  },
  {
    id: "offline-functionality",
    title: "Offline Processing",
    description: "Continue working without internet connectivity with our advanced offline processing capabilities.",
    icon: "📱",
    category: "Reliability",
    benefits: [
      "Full offline transcription",
      "Local data processing",
      "Automatic sync when online",
      "No data sent to servers",
      "Zero downtime"
    ],
    technicalDetails: "Edge computing with compressed AI models for local processing without compromising quality."
  },
  {
    id: "privacy-security",
    title: "Privacy-First Design",
    description: "Your data stays yours with end-to-end encryption and local processing options.",
    icon: "🔒",
    category: "Security",
    benefits: [
      "End-to-end encryption",
      "Local processing options",
      "No data storage on servers",
      "GDPR compliant",
      "SOC 2 Type II certified"
    ],
    technicalDetails: "AES-256 encryption with optional local-only processing modes for maximum privacy."
  },
  {
    id: "customizable-hotkeys",
    title: "Customizable Hotkeys",
    description: "Create custom keyboard shortcuts for instant voice recording and transcription control.",
    icon: "⌨️",
    category: "Productivity",
    benefits: [
      "Global system hotkeys",
      "Customizable shortcuts",
      "Quick action commands",
      "Workflow automation",
      "Multi-device sync"
    ],
    technicalDetails: "Advanced keyboard event handling with cross-platform compatibility."
  },
  {
    id: "advanced-analytics",
    title: "Advanced Analytics",
    description: "Gain insights into your voice patterns, usage statistics, and productivity metrics.",
    icon: "📊",
    category: "Analytics",
    benefits: [
      "Usage statistics",
      "Voice pattern analysis",
      "Productivity metrics",
      "Custom reporting",
      "Export capabilities"
    ],
    technicalDetails: "Comprehensive analytics engine with privacy-preserving data aggregation."
  },
  {
    id: "api-integrations",
    title: "API & Integrations",
    description: "Seamlessly integrate with your favorite tools and applications via our robust API.",
    icon: "🔗",
    category: "Integration",
    benefits: [
      "RESTful API",
      "Webhook support",
      "Popular app integrations",
      "Custom workflows",
      "Developer-friendly SDKs"
    ],
    technicalDetails: "Comprehensive REST API with SDKs for major programming languages and frameworks."
  }
];

// Use Cases Content
export const useCasesContent: UseCaseData[] = [
  {
    id: "content-creators",
    title: "Content Creators & Podcasters",
    description: "Transform your audio content into searchable, editable text with perfect accuracy.",
    industry: "Media & Entertainment",
    image: "/images/use-cases/content-creators.jpg",
    benefits: [
      "Instant podcast transcriptions",
      "SEO-friendly content generation",
      "Multi-language subtitle creation",
      "Real-time show notes",
      "Content repurposing"
    ],
    metrics: {
      efficiency: "80% faster content creation",
      accuracy: "99.5% transcription accuracy",
      timeSaved: "15+ hours per week",
      costSaved: "$2000+ monthly on transcription services"
    },
    testimonial: {
      quote: "Voice Input Assistant revolutionized our podcast workflow. What used to take hours now takes minutes.",
      author: "Sarah Johnson",
      company: "Tech Talk Podcast"
    }
  },
  {
    id: "business-professionals",
    title: "Business Professionals",
    description: "Streamline meetings, calls, and documentation with intelligent voice-to-text conversion.",
    industry: "Business & Enterprise",
    image: "/images/use-cases/business-professionals.jpg",
    benefits: [
      "Meeting transcription and summaries",
      "Voice-to-email dictation",
      "Call recording and analysis",
      "Document creation",
      "Action item extraction"
    ],
    metrics: {
      efficiency: "60% faster documentation",
      accuracy: "99% meeting capture rate",
      timeSaved: "10+ hours per week",
      costSaved: "$1500+ monthly on transcription"
    },
    testimonial: {
      quote: "Our team productivity skyrocketed after implementing Voice Input Assistant for all our meetings.",
      author: "Michael Chen",
      company: "Stellar Consulting"
    }
  },
  {
    id: "healthcare-providers",
    title: "Healthcare Providers",
    description: "Improve patient care with HIPAA-compliant voice documentation and clinical note-taking.",
    industry: "Healthcare",
    image: "/images/use-cases/healthcare.jpg",
    benefits: [
      "Clinical documentation",
      "Patient history recording",
      "Prescription dictation",
      "HIPAA-compliant processing",
      "Medical terminology recognition"
    ],
    metrics: {
      efficiency: "50% faster documentation",
      accuracy: "98% medical terminology accuracy",
      timeSaved: "2+ hours per day per provider",
      costSaved: "$5000+ annually per provider"
    },
    testimonial: {
      quote: "This tool has given me back hours each day to focus on what matters most - my patients.",
      author: "Dr. Amanda Rodriguez",
      company: "Metro Medical Center"
    }
  },
  {
    id: "students-researchers",
    title: "Students & Researchers",
    description: "Accelerate learning and research with intelligent lecture transcription and note-taking.",
    industry: "Education & Research",
    image: "/images/use-cases/education.jpg",
    benefits: [
      "Lecture transcription",
      "Interview documentation",
      "Research note organization",
      "Multi-language support",
      "Citation extraction"
    ],
    metrics: {
      efficiency: "70% faster note-taking",
      accuracy: "97% lecture capture accuracy",
      timeSaved: "5+ hours per week",
      costSaved: "$800+ per semester"
    },
    testimonial: {
      quote: "I never miss important details in lectures anymore. This is a game-changer for students.",
      author: "Emma Thompson",
      company: "Stanford University"
    }
  },
  {
    id: "legal-professionals",
    title: "Legal Professionals",
    description: "Enhance legal workflows with secure, accurate transcription of depositions and client meetings.",
    industry: "Legal Services",
    image: "/images/use-cases/legal.jpg",
    benefits: [
      "Deposition transcription",
      "Client meeting documentation",
      "Legal document dictation",
      "Confidential processing",
      "Timestamp accuracy"
    ],
    metrics: {
      efficiency: "65% faster case documentation",
      accuracy: "99.8% legal transcription accuracy",
      timeSaved: "12+ hours per week",
      costSaved: "$3000+ monthly on court reporters"
    },
    testimonial: {
      quote: "The accuracy and speed have transformed how we handle client documentation and case preparation.",
      author: "James Martinez",
      company: "Martinez & Associates"
    }
  },
  {
    id: "accessibility-users",
    title: "Accessibility & Inclusion",
    description: "Empower users with hearing difficulties through real-time transcription and visual feedback.",
    industry: "Accessibility",
    image: "/images/use-cases/accessibility.jpg",
    benefits: [
      "Real-time conversation transcription",
      "Meeting accessibility",
      "Visual audio feedback",
      "Custom vocabulary",
      "Large text support"
    ],
    metrics: {
      efficiency: "100% meeting participation",
      accuracy: "96% conversational accuracy",
      timeSaved: "Immediate comprehension",
      costSaved: "Eliminates need for interpreters"
    },
    testimonial: {
      quote: "This tool has opened up a world of opportunities for me in my professional life.",
      author: "David Kim",
      company: "Inclusive Tech Solutions"
    }
  }
];

// Testimonials Content
export const testimonialsContent: TestimonialData[] = [
  {
    id: "testimonial-1",
    name: "Sarah Johnson",
    title: "Podcast Host",
    company: "Tech Talk Podcast",
    image: "/images/testimonials/sarah-johnson.jpg",
    rating: 5,
    testimonial: "Voice Input Assistant has completely revolutionized our podcast production workflow. What used to take our team hours of manual transcription now takes just minutes. The accuracy is incredible, and the emotion detection feature helps us understand our audience's reactions better than ever before. It's not just a tool, it's a game-changer for content creators.",
    featured: true,
    dateSubmitted: "2024-01-15",
    verified: true,
    location: "San Francisco, CA",
    useCase: "Podcast Transcription",
    industry: "Media & Entertainment"
  },
  {
    id: "testimonial-2",
    name: "Dr. Amanda Rodriguez",
    title: "Physician",
    company: "Metro Medical Center",
    image: "/images/testimonials/amanda-rodriguez.jpg",
    rating: 5,
    testimonial: "As a busy physician, every minute counts. This voice assistant has given me back precious time to focus on patient care instead of endless documentation. The HIPAA compliance and medical terminology accuracy are outstanding. My colleagues are amazed at how much more efficient my practice has become.",
    featured: true,
    dateSubmitted: "2024-01-20",
    verified: true,
    location: "Chicago, IL",
    useCase: "Medical Documentation",
    industry: "Healthcare"
  },
  {
    id: "testimonial-3",
    name: "Michael Chen",
    title: "CEO",
    company: "Stellar Consulting",
    image: "/images/testimonials/michael-chen.jpg",
    rating: 5,
    testimonial: "Our team productivity has skyrocketed since implementing Voice Input Assistant across our organization. The meeting transcriptions are incredibly accurate, and the action item extraction feature ensures nothing falls through the cracks. The ROI has been phenomenal - we've saved thousands in transcription costs while improving our documentation quality.",
    featured: true,
    dateSubmitted: "2024-02-01",
    verified: true,
    location: "New York, NY",
    useCase: "Business Meetings",
    industry: "Business Consulting"
  },
  {
    id: "testimonial-4",
    name: "Emma Thompson",
    title: "Graduate Student",
    company: "Stanford University",
    image: "/images/testimonials/emma-thompson.jpg",
    rating: 5,
    testimonial: "This tool has been a lifesaver during my graduate studies. I never miss important details in lectures anymore, and the multi-language support helped me with international research interviews. The offline functionality means I can use it anywhere on campus without worrying about connectivity. Absolutely essential for any serious student.",
    featured: false,
    dateSubmitted: "2024-02-10",
    verified: true,
    location: "Stanford, CA",
    useCase: "Academic Research",
    industry: "Education"
  },
  {
    id: "testimonial-5",
    name: "James Martinez",
    title: "Managing Partner",
    company: "Martinez & Associates",
    image: "/images/testimonials/james-martinez.jpg",
    rating: 5,
    testimonial: "The accuracy and speed of transcription have transformed how we handle client documentation and case preparation. The security features give us complete confidence when dealing with confidential legal matters. We've reduced our court reporter costs by over 70% while improving our documentation turnaround time.",
    featured: false,
    dateSubmitted: "2024-02-15",
    verified: true,
    location: "Los Angeles, CA",
    useCase: "Legal Documentation",
    industry: "Legal Services"
  },
  {
    id: "testimonial-6",
    name: "David Kim",
    title: "Software Engineer",
    company: "Inclusive Tech Solutions",
    image: "/images/testimonials/david-kim.jpg",
    rating: 5,
    testimonial: "As someone with hearing difficulties, this tool has opened up a world of opportunities in my professional life. The real-time transcription during meetings ensures I never miss important discussions, and the visual feedback helps me participate fully in team collaborations. It's more than assistive technology - it's empowering technology.",
    featured: false,
    dateSubmitted: "2024-02-20",
    verified: true,
    location: "Seattle, WA",
    useCase: "Accessibility",
    industry: "Technology"
  },
  {
    id: "testimonial-7",
    name: "Lisa Park",
    title: "Marketing Director",
    company: "Creative Solutions Inc.",
    image: "/images/testimonials/lisa-park.jpg",
    rating: 4,
    testimonial: "The voice-to-text accuracy is impressive, especially for marketing content creation. We use it for brainstorming sessions, client calls, and content ideation. The emotion detection feature provides valuable insights into our team's creative process. It's become an integral part of our creative workflow.",
    featured: false,
    dateSubmitted: "2024-03-01",
    verified: true,
    location: "Austin, TX",
    useCase: "Content Creation",
    industry: "Marketing & Advertising"
  },
  {
    id: "testimonial-8",
    name: "Robert Wilson",
    title: "Journalist",
    company: "Global News Network",
    image: "/images/testimonials/robert-wilson.jpg",
    rating: 5,
    testimonial: "Field reporting has never been easier. The offline functionality means I can record and transcribe interviews anywhere in the world, and the multi-language support is crucial for international stories. The transcription speed allows me to meet tight deadlines while maintaining accuracy in my reporting.",
    featured: false,
    dateSubmitted: "2024-03-05",
    verified: true,
    location: "London, UK",
    useCase: "Journalism",
    industry: "Media & News"
  }
];

// Pricing Plans Content
export const pricingPlans: PricingPlan[] = [
  {
    id: "starter",
    name: "Starter",
    description: "Perfect for individuals getting started with voice transcription",
    price: 0,
    currency: "USD",
    interval: "month",
    features: [
      "60 minutes of transcription per month",
      "Real-time voice-to-text",
      "Basic emotion detection",
      "5 custom hotkeys",
      "Standard accuracy (95%)",
      "Email support",
      "Web app access",
      "Export to text/PDF"
    ],
    limitations: [
      "Limited to 60 minutes/month",
      "No offline functionality",
      "Basic analytics only",
      "Single device",
      "Community support only"
    ],
    popular: false,
    ctaText: "Start Free",
    ctaUrl: "/signup?plan=starter",
    trialDays: 0
  },
  {
    id: "professional",
    name: "Professional",
    description: "Ideal for professionals, content creators, and small teams",
    price: 29,
    currency: "USD",
    interval: "month",
    features: [
      "500 minutes of transcription per month",
      "Premium accuracy (99.5%)",
      "Advanced emotion & sentiment analysis",
      "Unlimited custom hotkeys",
      "Offline transcription",
      "Multi-language support (50+ languages)",
      "API access",
      "Priority support",
      "Advanced analytics",
      "Team collaboration (up to 3 users)",
      "Custom vocabulary",
      "Export to multiple formats"
    ],
    popular: true,
    ctaText: "Start Professional",
    ctaUrl: "/signup?plan=professional",
    trialDays: 14,
    stripePriceId: "price_professional_monthly"
  },
  {
    id: "business",
    name: "Business",
    description: "Comprehensive solution for growing businesses and larger teams",
    price: 99,
    currency: "USD",
    interval: "month",
    features: [
      "2000 minutes of transcription per month",
      "Premium accuracy (99.5%)",
      "Full AI/ML feature suite",
      "Unlimited hotkeys & workflows",
      "Advanced offline capabilities",
      "All languages supported",
      "Full API access with webhooks",
      "24/7 priority support",
      "Advanced analytics & reporting",
      "Team management (up to 15 users)",
      "Custom integrations",
      "SSO (Single Sign-On)",
      "Admin dashboard",
      "Bulk operations",
      "Custom branding"
    ],
    ctaText: "Start Business",
    ctaUrl: "/signup?plan=business",
    trialDays: 30,
    stripePriceId: "price_business_monthly"
  },
  {
    id: "enterprise",
    name: "Enterprise",
    description: "Custom solution for large organizations with specific requirements",
    price: 299,
    currency: "USD",
    interval: "month",
    features: [
      "Unlimited transcription",
      "Premium accuracy with custom models",
      "Full feature access",
      "Custom AI model training",
      "On-premise deployment options",
      "All languages + custom languages",
      "Custom API endpoints",
      "Dedicated support team",
      "Custom analytics & reporting",
      "Unlimited users",
      "Custom integrations & workflows",
      "Advanced security & compliance",
      "Custom SLA",
      "White-label options",
      "Professional services"
    ],
    ctaText: "Contact Sales",
    ctaUrl: "/contact?plan=enterprise",
    trialDays: 30,
    stripePriceId: "price_enterprise_monthly"
  }
];

// FAQ Content
export const faqContent = [
  {
    question: "How accurate is the voice transcription?",
    answer: "Our voice transcription achieves 99.5% accuracy in optimal conditions. The accuracy depends on factors like audio quality, background noise, and speaker clarity. Our AI continuously learns and improves, and we support custom vocabulary to enhance accuracy for specific domains.",
    category: "accuracy"
  },
  {
    question: "Can I use Voice Input Assistant offline?",
    answer: "Yes! Our Professional and higher plans include offline functionality. You can transcribe voice to text without an internet connection, and your data will automatically sync when you're back online. This feature uses locally-stored AI models for complete privacy.",
    category: "features"
  },
  {
    question: "Is my voice data secure and private?",
    answer: "Absolutely. We use end-to-end encryption for all voice data transmission. With offline processing, your audio never leaves your device. We're GDPR compliant, SOC 2 Type II certified, and offer local-only processing options. Your privacy is our top priority.",
    category: "privacy"
  },
  {
    question: "What languages are supported?",
    answer: "We support 50+ languages including English, Spanish, French, German, Italian, Portuguese, Japanese, Korean, Chinese (Mandarin & Cantonese), Arabic, Hindi, and many more. Our AI automatically detects the language being spoken and can handle multiple languages in a single session.",
    category: "features"
  },
  {
    question: "How does the emotion detection work?",
    answer: "Our AI analyzes vocal patterns, tone, and speech characteristics to identify emotions like happiness, sadness, anger, surprise, and more. It provides confidence scores and can track emotional changes over time. This feature is perfect for content analysis, customer service, and accessibility applications.",
    category: "ai"
  },
  {
    question: "Can I integrate Voice Input Assistant with other applications?",
    answer: "Yes! We provide a comprehensive REST API with webhooks, SDKs for popular programming languages, and direct integrations with tools like Slack, Microsoft Teams, Google Workspace, and more. Our Business and Enterprise plans include custom integration support.",
    category: "integration"
  },
  {
    question: "What devices and platforms are supported?",
    answer: "Voice Input Assistant works on all modern web browsers (Chrome, Firefox, Safari, Edge), as a PWA on mobile devices (iOS, Android), and we offer native desktop applications for Windows, macOS, and Linux. All your data syncs seamlessly across devices.",
    category: "compatibility"
  },
  {
    question: "Is there a free trial available?",
    answer: "Yes! Our Starter plan is completely free with 60 minutes of transcription per month. Professional and Business plans include 14-day and 30-day free trials respectively. Enterprise customers get a 30-day trial with full feature access and dedicated support.",
    category: "pricing"
  },
  {
    question: "How does pricing work and can I change plans?",
    answer: "Our pricing is based on monthly transcription minutes and features. You can upgrade, downgrade, or cancel anytime. Unused minutes don't roll over, but you can purchase additional minutes as needed. Enterprise customers can get custom pricing based on their specific requirements.",
    category: "pricing"
  },
  {
    question: "What kind of support do you provide?",
    answer: "We offer multiple support channels: email support for all users, priority support for Professional+ plans, 24/7 support for Business+ plans, and dedicated support teams for Enterprise customers. We also have comprehensive documentation, tutorials, and a community forum.",
    category: "support"
  },
  {
    question: "Can Voice Input Assistant handle technical or specialized terminology?",
    answer: "Absolutely! You can create custom vocabularies with technical terms, proper nouns, and industry-specific language. Our AI learns from your corrections and improves accuracy over time. We have specialized models for medical, legal, technical, and other professional domains.",
    category: "customization"
  },
  {
    question: "How does the real-time transcription work?",
    answer: "Our real-time transcription uses advanced WebRTC technology and optimized AI models to process speech as you speak. There's typically less than 100ms latency, making it perfect for live meetings, presentations, and real-time collaboration. The transcription appears instantly as you talk.",
    category: "technology"
  }
];

// Benefits Content
export const benefitsContent = {
  title: "Why Choose Voice Input Assistant?",
  subtitle: "Discover the advantages that make us the leading voice transcription solution",
  benefits: [
    {
      icon: "🚀",
      title: "10x Faster Documentation",
      description: "Transform hours of typing into minutes of speaking. Our users report saving 10-15 hours per week on documentation tasks.",
      stats: "Average 10x speed improvement"
    },
    {
      icon: "🎯",
      title: "Industry-Leading Accuracy",
      description: "99.5% transcription accuracy with advanced AI models trained on millions of hours of diverse speech patterns.",
      stats: "99.5% accuracy rate"
    },
    {
      icon: "💰",
      title: "Significant Cost Savings",
      description: "Reduce transcription costs by up to 80% compared to traditional services while improving speed and quality.",
      stats: "80% cost reduction"
    },
    {
      icon: "🔒",
      title: "Privacy & Security First",
      description: "End-to-end encryption, local processing options, and enterprise-grade security ensure your data stays protected.",
      stats: "SOC 2 Type II certified"
    },
    {
      icon: "🌐",
      title: "Works Anywhere",
      description: "Full offline functionality means you can transcribe anywhere, anytime, without internet connectivity.",
      stats: "100% uptime offline"
    },
    {
      icon: "⚡",
      title: "Real-Time Processing",
      description: "See your words appear instantly as you speak with our optimized real-time transcription engine.",
      stats: "<100ms latency"
    }
  ]
};

// Call-to-Action Content
export const ctaContent = {
  title: "Ready to Transform Your Voice Into Intelligent Text?",
  subtitle: "Join thousands of professionals who have revolutionized their workflow with Voice Input Assistant",
  description: "Start your free trial today and experience the future of voice transcription. No credit card required.",
  buttons: {
    primary: {
      text: "Start Free Trial",
      link: "/signup"
    },
    secondary: {
      text: "Schedule Demo",
      link: "/demo"
    }
  },
  guarantee: "30-day money-back guarantee • Cancel anytime • No setup fees",
  stats: [
    { number: "10,000+", label: "Happy Users" },
    { number: "1M+", label: "Hours Transcribed" },
    { number: "99.5%", label: "Accuracy Rate" },
    { number: "4.8/5", label: "User Rating" }
  ]
};

// Social Proof Content
export const socialProofContent = {
  title: "Trusted by Industry Leaders",
  companies: [
    { name: "Microsoft", logo: "/images/companies/microsoft.svg" },
    { name: "Google", logo: "/images/companies/google.svg" },
    { name: "Amazon", logo: "/images/companies/amazon.svg" },
    { name: "Apple", logo: "/images/companies/apple.svg" },
    { name: "Meta", logo: "/images/companies/meta.svg" },
    { name: "Netflix", logo: "/images/companies/netflix.svg" },
    { name: "Spotify", logo: "/images/companies/spotify.svg" },
    { name: "Adobe", logo: "/images/companies/adobe.svg" }
  ],
  stats: {
    title: "The Numbers Speak for Themselves",
    metrics: [
      {
        number: "10,000+",
        label: "Active Users",
        description: "Professionals and businesses worldwide"
      },
      {
        number: "1M+",
        label: "Hours Transcribed",
        description: "Of audio content processed accurately"
      },
      {
        number: "99.5%",
        label: "Accuracy Rate",
        description: "Industry-leading transcription precision"
      },
      {
        number: "4.8/5",
        label: "User Rating",
        description: "Based on 500+ verified reviews"
      }
    ]
  }
};

// Export all content
export const marketingContent = {
  hero: heroContent,
  features: featuresContent,
  benefits: benefitsContent,
  useCases: useCasesContent,
  testimonials: testimonialsContent,
  pricing: pricingPlans,
  faq: faqContent,
  cta: ctaContent,
  socialProof: socialProofContent
};