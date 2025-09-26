import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import Link from 'next/link';
import Image from 'next/image';
import './globals.css';

const inter = Inter({ subsets: ['latin'] });

const navigation = {
  product: [
    { name: 'Features', href: '/features' },
    { name: 'Download', href: '/download' },
    { name: 'Pricing', href: '/pricing' },
    { name: 'Changelog', href: '/changelog' },
  ],
  resources: [
    { name: 'Documentation', href: '/docs' },
    { name: 'API Reference', href: '/docs/api' },
    { name: 'Blog', href: '/blog' },
    { name: 'Help Center', href: '/help' },
  ],
  company: [
    { name: 'About', href: '/about' },
    { name: 'Contact', href: '/contact' },
    { name: 'Careers', href: '/careers' },
    { name: 'Press Kit', href: '/press' },
  ],
  legal: [
    { name: 'Privacy', href: '/privacy' },
    { name: 'Terms', href: '/terms' },
    { name: 'Security', href: '/security' },
    { name: 'Compliance', href: '/compliance' },
  ],
};

export const metadata: Metadata = {
  title: 'Voice Input Assistant - AI-Powered Speech Recognition',
  description: 'Transform your voice into text instantly with our AI-powered speech recognition software. Works everywhere on Windows.',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className={inter.className}>
        {/* Navigation */}
        <nav className="bg-gray-900 border-b border-gray-800">
          <div className="container mx-auto px-4">
            <div className="flex items-center justify-between h-16">
              <div className="flex items-center">
                <Link href="/" className="flex items-center">
                  <Image
                    src="/logo.svg"
                    alt="Voice Input Assistant"
                    width={40}
                    height={40}
                    className="mr-2"
                  />
                  <span className="text-white font-bold text-lg">
                    Voice Input Assistant
                  </span>
                </Link>
              </div>
              <div className="hidden md:flex items-center space-x-8">
                <Link
                  href="/features"
                  className="text-gray-300 hover:text-white transition"
                >
                  Features
                </Link>
                <Link
                  href="/download"
                  className="text-gray-300 hover:text-white transition"
                >
                  Download
                </Link>
                <Link
                  href="/pricing"
                  className="text-gray-300 hover:text-white transition"
                >
                  Pricing
                </Link>
                <Link
                  href="/docs"
                  className="text-gray-300 hover:text-white transition"
                >
                  Docs
                </Link>
                <Link
                  href="/blog"
                  className="text-gray-300 hover:text-white transition"
                >
                  Blog
                </Link>
                <Link
                  href="https://app.voiceinputassistant.com"
                  className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg transition"
                >
                  Sign In
                </Link>
              </div>
              <button className="md:hidden text-gray-300 hover:text-white">
                <svg
                  className="h-6 w-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M4 6h16M4 12h16M4 18h16"
                  />
                </svg>
              </button>
            </div>
          </div>
        </nav>

        {children}

        {/* Footer */}
        <footer className="bg-gray-900 text-gray-400 py-12">
          <div className="container mx-auto px-4">
            <div className="grid md:grid-cols-2 lg:grid-cols-5 gap-8">
              <div className="col-span-2">
                <Link href="/" className="flex items-center mb-6">
                  <Image
                    src="/logo.svg"
                    alt="Voice Input Assistant"
                    width={40}
                    height={40}
                    className="mr-2"
                  />
                  <span className="text-white font-bold text-lg">
                    Voice Input Assistant
                  </span>
                </Link>
                <p className="text-gray-400 mb-8 max-w-md">
                  Transform your voice into text instantly with AI-powered accuracy.
                  Works everywhere on Windows.
                </p>
                <div className="flex space-x-6">
                  <a href="https://twitter.com/voiceinputai" className="text-gray-400 hover:text-white">
                    <span className="sr-only">Twitter</span>
                    <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M8.29 20.251c7.547 0 11.675-6.253 11.675-11.675 0-.178 0-.355-.012-.53A8.348 8.348 0 0022 5.92a8.19 8.19 0 01-2.357.646 4.118 4.118 0 001.804-2.27 8.224 8.224 0 01-2.605.996 4.107 4.107 0 00-6.993 3.743 11.65 11.65 0 01-8.457-4.287 4.106 4.106 0 001.27 5.477A4.072 4.072 0 012.8 9.713v.052a4.105 4.105 0 003.292 4.022 4.095 4.095 0 01-1.853.07 4.108 4.108 0 003.834 2.85A8.233 8.233 0 012 18.407a11.616 11.616 0 006.29 1.84" />
                    </svg>
                  </a>
                  <a href="https://github.com/voiceinputai" className="text-gray-400 hover:text-white">
                    <span className="sr-only">GitHub</span>
                    <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 24 24">
                      <path fillRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" clipRule="evenodd" />
                    </svg>
                  </a>
                </div>
              </div>
              <div>
                <h3 className="text-white font-semibold mb-4">Product</h3>
                <ul className="space-y-2">
                  {navigation.product.map((item) => (
                    <li key={item.name}>
                      <Link
                        href={item.href}
                        className="text-gray-400 hover:text-white transition"
                      >
                        {item.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
              <div>
                <h3 className="text-white font-semibold mb-4">Resources</h3>
                <ul className="space-y-2">
                  {navigation.resources.map((item) => (
                    <li key={item.name}>
                      <Link
                        href={item.href}
                        className="text-gray-400 hover:text-white transition"
                      >
                        {item.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
              <div>
                <h3 className="text-white font-semibold mb-4">Company</h3>
                <ul className="space-y-2">
                  {navigation.company.map((item) => (
                    <li key={item.name}>
                      <Link
                        href={item.href}
                        className="text-gray-400 hover:text-white transition"
                      >
                        {item.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
            <div className="border-t border-gray-800 mt-12 pt-8">
              <div className="grid sm:grid-cols-2 gap-8">
                <p className="text-gray-400">
                  © {new Date().getFullYear()} Voice Input Assistant. All rights
                  reserved.
                </p>
                <div className="sm:text-right">
                  <ul className="flex space-x-6 sm:justify-end">
                    {navigation.legal.map((item) => (
                      <li key={item.name}>
                        <Link
                          href={item.href}
                          className="text-gray-400 hover:text-white transition"
                        >
                          {item.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </footer>
      </body>
    </html>
  );
}
