import Image from "next/image";

export default function Home() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-900 to-gray-800">
      {/* Hero Section */}
      <section className="py-20 text-center text-white">
        <div className="container mx-auto px-4">
          <Image
            src="/logo.svg"
            alt="Voice Input Assistant"
            width={180}
            height={60}
            className="mx-auto mb-8"
            priority
          />
          <h1 className="text-5xl md:text-7xl font-bold mb-8">
            Your Voice,{' '}
            <span className="bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-500">
              Anywhere
            </span>
          </h1>
          <p className="text-xl md:text-2xl text-gray-300 mb-12">
            Revolutionary speech-to-text software that works everywhere on Windows
            with AI-powered accuracy.
          </p>
          <div className="flex flex-col md:flex-row gap-6 justify-center">
            <a
              href="/download"
              className="bg-blue-600 hover:bg-blue-700 text-white font-bold py-4 px-8 rounded-lg transition"
            >
              Download Now
            </a>
            <a
              href="#pricing"
              className="bg-gray-700 hover:bg-gray-600 text-white font-bold py-4 px-8 rounded-lg transition"
            >
              View Pricing
            </a>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="py-10 bg-gray-800/50 border-y border-gray-700">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
            <div className="text-white">
              <div className="text-4xl font-bold mb-2">50K+</div>
              <div className="text-gray-400">Active Users</div>
            </div>
            <div className="text-white">
              <div className="text-4xl font-bold mb-2">99.9%</div>
              <div className="text-gray-400">Uptime</div>
            </div>
            <div className="text-white">
              <div className="text-4xl font-bold mb-2">95%</div>
              <div className="text-gray-400">Accuracy</div>
            </div>
            <div className="text-white">
              <div className="text-4xl font-bold mb-2">24/7</div>
              <div className="text-gray-400">Support</div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20 bg-gray-800/30">
        <div className="container mx-auto px-4">
          <h2 className="text-4xl font-bold text-center text-white mb-16">
            Why Choose Voice Input Assistant?
          </h2>
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            {/* Feature Cards */}
            {[
              {
                title: 'Works Everywhere',
                description: 'Use voice input in any Windows application - from Word to Chrome to custom software.',
                icon: '🎯'
              },
              {
                title: 'AI-Powered Accuracy',
                description: 'Advanced machine learning ensures high accuracy and natural language understanding.',
                icon: '🧠'
              },
              {
                title: 'Privacy First',
                description: 'Process everything locally or choose cloud processing for maximum accuracy.',
                icon: '🔒'
              },
              {
                title: 'Custom Vocabularies',
                description: 'Train the system with industry-specific terms and your preferences.',
                icon: '📚'
              },
              {
                title: 'Real-time Processing',
                description: 'See your words appear instantly as you speak with minimal latency.',
                icon: '⚡'
              },
              {
                title: 'Easy Integration',
                description: 'Simple API and SDK for developers to add voice input to any application.',
                icon: '🔌'
              }
            ].map((feature, index) => (
              <div key={index} className="bg-gray-800 rounded-lg p-8 hover:bg-gray-750 transition-colors">
                <div className="text-4xl mb-4">{feature.icon}</div>
                <h3 className="text-xl font-bold text-white mb-4">{feature.title}</h3>
                <p className="text-gray-400">{feature.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section id="pricing" className="py-20 bg-gray-900">
        <div className="container mx-auto px-4">
          <h2 className="text-4xl font-bold text-center text-white mb-16">
            Simple, Transparent Pricing
          </h2>
          <div className="grid md:grid-cols-3 gap-8">
            {/* Free Tier */}
            <div className="bg-gray-800 rounded-lg p-8 text-white">
              <h3 className="text-2xl font-bold mb-4">Free</h3>
              <div className="text-4xl font-bold mb-8">$0</div>
              <ul className="space-y-4 mb-8 text-gray-300">
                <li>✓ Basic voice recognition</li>
                <li>✓ Local processing</li>
                <li>✓ Windows system-wide support</li>
                <li>✓ Community support</li>
              </ul>
              <a
                href="/download"
                className="block text-center bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 px-6 rounded-lg transition"
              >
                Download Now
              </a>
            </div>

            {/* Pro Tier */}
            <div className="bg-blue-600 rounded-lg p-8 text-white transform scale-105 shadow-xl">
              <h3 className="text-2xl font-bold mb-4">Pro</h3>
              <div className="text-4xl font-bold mb-8">$9.99/mo</div>
              <ul className="space-y-4 mb-8">
                <li>✓ Everything in Free</li>
                <li>✓ Cloud processing</li>
                <li>✓ Advanced AI features</li>
                <li>✓ Custom vocabularies</li>
                <li>✓ Priority support</li>
              </ul>
              <a
                href="/purchase/pro"
                className="block text-center bg-white text-blue-600 hover:bg-gray-100 font-bold py-3 px-6 rounded-lg transition"
              >
                Get Pro
              </a>
            </div>

            {/* Enterprise Tier */}
            <div className="bg-gray-800 rounded-lg p-8 text-white">
              <h3 className="text-2xl font-bold mb-4">Enterprise</h3>
              <div className="text-4xl font-bold mb-8">Custom</div>
              <ul className="space-y-4 mb-8 text-gray-300">
                <li>✓ Everything in Pro</li>
                <li>✓ Custom deployment</li>
                <li>✓ SSO & advanced security</li>
                <li>✓ SLA guarantee</li>
                <li>✓ Dedicated support</li>
              </ul>
              <a
                href="/contact"
                className="block text-center bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 px-6 rounded-lg transition"
              >
                Contact Sales
              </a>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20 bg-gradient-to-r from-blue-600 to-purple-600">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-4xl font-bold text-white mb-8">
            Ready to Transform Your Workflow?
          </h2>
          <p className="text-xl text-white mb-12">
            Join thousands of users who've revolutionized their productivity with
            Voice Input Assistant.
          </p>
          <div className="flex flex-col md:flex-row gap-6 justify-center">
            <a
              href="/download"
              className="bg-white text-blue-600 hover:bg-gray-100 font-bold py-4 px-8 rounded-lg transition"
            >
              Get Started Free
            </a>
            <a
              href="/contact"
              className="bg-transparent border-2 border-white text-white hover:bg-white/10 font-bold py-4 px-8 rounded-lg transition"
            >
              Schedule Demo
            </a>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-gray-400 py-12 border-t border-gray-800">
        <div className="container mx-auto px-4">
          <div className="grid md:grid-cols-4 gap-8">
            <div>
              <Image
                src="/logo.svg"
                alt="Voice Input Assistant"
                width={120}
                height={40}
                className="mb-4"
              />
              <p className="text-sm">
                Revolutionizing voice-to-text with AI-powered accuracy.
              </p>
            </div>
            <div>
              <h3 className="text-white text-lg font-bold mb-4">Product</h3>
              <ul className="space-y-2">
                <li><a href="/download" className="hover:text-white">Download</a></li>
                <li><a href="/pricing" className="hover:text-white">Pricing</a></li>
                <li><a href="/features" className="hover:text-white">Features</a></li>
                <li><a href="/roadmap" className="hover:text-white">Roadmap</a></li>
              </ul>
            </div>
            <div>
              <h3 className="text-white text-lg font-bold mb-4">Resources</h3>
              <ul className="space-y-2">
                <li><a href="/docs" className="hover:text-white">Documentation</a></li>
                <li><a href="/api" className="hover:text-white">API Reference</a></li>
                <li><a href="/blog" className="hover:text-white">Blog</a></li>
                <li><a href="/help" className="hover:text-white">Help Center</a></li>
              </ul>
            </div>
            <div>
              <h3 className="text-white text-lg font-bold mb-4">Company</h3>
              <ul className="space-y-2">
                <li><a href="/about" className="hover:text-white">About</a></li>
                <li><a href="/contact" className="hover:text-white">Contact</a></li>
                <li><a href="/legal" className="hover:text-white">Legal</a></li>
                <li><a href="/privacy" className="hover:text-white">Privacy</a></li>
              </ul>
            </div>
          </div>
          <div className="border-t border-gray-800 mt-12 pt-8 text-sm text-center">
            <p>© 2024 Voice Input Assistant. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
