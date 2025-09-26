import Image from 'next/image';

export default function Pricing() {
  const featuresByTier = {
    free: [
      { name: 'Basic voice recognition', included: true },
      { name: 'Local processing', included: true },
      { name: 'Windows system-wide support', included: true },
      { name: 'Single device', included: true },
      { name: 'Community support', included: true },
      { name: 'Cloud processing', included: false },
      { name: 'Custom vocabularies', included: false },
      { name: 'API access', included: false },
      { name: 'Priority support', included: false },
      { name: 'Multiple devices', included: false }
    ],
    pro: [
      { name: 'Everything in Free', included: true },
      { name: 'Cloud processing', included: true },
      { name: 'Custom vocabularies', included: true },
      { name: 'API access (10k requests/mo)', included: true },
      { name: 'Priority support', included: true },
      { name: 'Up to 3 devices', included: true },
      { name: 'Advanced AI features', included: true },
      { name: 'Usage analytics', included: true },
      { name: 'Team sharing', included: false },
      { name: 'Custom deployment', included: false }
    ],
    enterprise: [
      { name: 'Everything in Pro', included: true },
      { name: 'Unlimited devices', included: true },
      { name: 'Unlimited API access', included: true },
      { name: 'Custom deployment', included: true },
      { name: 'Team sharing', included: true },
      { name: 'SSO integration', included: true },
      { name: 'Advanced security', included: true },
      { name: 'SLA guarantee', included: true },
      { name: 'Dedicated account manager', included: true },
      { name: 'Custom integrations', included: true }
    ]
  };

  const monthlyPrices = {
    free: '0',
    pro: '9.99',
    enterprise: 'Custom'
  };

  const planCTAs = {
    free: {
      text: 'Download Now',
      link: '/download',
      style: 'bg-blue-600 hover:bg-blue-700'
    },
    pro: {
      text: 'Get Pro',
      link: '/purchase/pro',
      style: 'bg-white text-blue-600 hover:bg-gray-100'
    },
    enterprise: {
      text: 'Contact Sales',
      link: '/contact',
      style: 'bg-blue-600 hover:bg-blue-700'
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-900 to-gray-800">
      <main className="container mx-auto px-4 py-20">
        {/* Header */}
        <div className="text-center mb-16">
          <h1 className="text-4xl md:text-6xl font-bold text-white mb-6">
            Simple, Transparent Pricing
          </h1>
          <p className="text-xl text-gray-300 mb-12 max-w-3xl mx-auto">
            Choose the perfect plan for your needs. All plans include our core
            voice-to-text technology.
          </p>
        </div>

        {/* Pricing Table */}
        <div className="grid lg:grid-cols-3 gap-8 mb-16">
          {/* Free Tier */}
          <div className="bg-gray-800 rounded-lg p-8">
            <div className="text-center mb-8">
              <h2 className="text-2xl font-bold text-white mb-4">Free</h2>
              <div className="text-5xl font-bold text-white mb-4">
                ${monthlyPrices.free}
              </div>
              <p className="text-gray-400">Perfect for getting started</p>
            </div>
            <ul className="space-y-4 mb-8">
              {featuresByTier.free.map((feature, index) => (
                <li
                  key={index}
                  className={`flex items-center ${
                    feature.included ? 'text-gray-300' : 'text-gray-500'
                  }`}
                >
                  {feature.included ? (
                    <svg
                      className="w-5 h-5 mr-2 text-green-400"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                  ) : (
                    <svg
                      className="w-5 h-5 mr-2"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M6 18L18 6M6 6l12 12"
                      />
                    </svg>
                  )}
                  {feature.name}
                </li>
              ))}
            </ul>
            <a
              href={planCTAs.free.link}
              className={`block w-full text-center text-white font-bold py-3 px-6 rounded-lg transition ${planCTAs.free.style}`}
            >
              {planCTAs.free.text}
            </a>
          </div>

          {/* Pro Tier */}
          <div className="bg-blue-600 rounded-lg p-8 transform lg:scale-105 lg:-translate-y-4">
            <div className="text-center mb-8">
              <div className="bg-blue-500 text-white text-sm font-bold py-1 px-3 rounded-full inline-block mb-4">
                MOST POPULAR
              </div>
              <h2 className="text-2xl font-bold text-white mb-4">Pro</h2>
              <div className="text-5xl font-bold text-white mb-4">
                ${monthlyPrices.pro}
                <span className="text-lg">/mo</span>
              </div>
              <p className="text-blue-100">For power users</p>
            </div>
            <ul className="space-y-4 mb-8">
              {featuresByTier.pro.map((feature, index) => (
                <li
                  key={index}
                  className={`flex items-center ${
                    feature.included ? 'text-white' : 'text-blue-300'
                  }`}
                >
                  {feature.included ? (
                    <svg
                      className="w-5 h-5 mr-2 text-white"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                  ) : (
                    <svg
                      className="w-5 h-5 mr-2"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M6 18L18 6M6 6l12 12"
                      />
                    </svg>
                  )}
                  {feature.name}
                </li>
              ))}
            </ul>
            <a
              href={planCTAs.pro.link}
              className={`block w-full text-center font-bold py-3 px-6 rounded-lg transition ${planCTAs.pro.style}`}
            >
              {planCTAs.pro.text}
            </a>
          </div>

          {/* Enterprise Tier */}
          <div className="bg-gray-800 rounded-lg p-8">
            <div className="text-center mb-8">
              <h2 className="text-2xl font-bold text-white mb-4">Enterprise</h2>
              <div className="text-5xl font-bold text-white mb-4">
                {monthlyPrices.enterprise}
              </div>
              <p className="text-gray-400">For organizations</p>
            </div>
            <ul className="space-y-4 mb-8">
              {featuresByTier.enterprise.map((feature, index) => (
                <li
                  key={index}
                  className="flex items-center text-gray-300"
                >
                  <svg
                    className="w-5 h-5 mr-2 text-green-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M5 13l4 4L19 7"
                    />
                  </svg>
                  {feature.name}
                </li>
              ))}
            </ul>
            <a
              href={planCTAs.enterprise.link}
              className={`block w-full text-center text-white font-bold py-3 px-6 rounded-lg transition ${planCTAs.enterprise.style}`}
            >
              {planCTAs.enterprise.text}
            </a>
          </div>
        </div>

        {/* FAQ */}
        <div className="bg-gray-800 rounded-lg p-8">
          <h2 className="text-2xl font-bold text-white mb-8">
            Frequently Asked Questions
          </h2>
          <div className="grid md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-lg font-bold text-white mb-2">
                Can I change plans at any time?
              </h3>
              <p className="text-gray-400">
                Yes, you can upgrade, downgrade, or cancel your subscription at any
                time. Changes take effect at the start of the next billing cycle.
              </p>
            </div>
            <div>
              <h3 className="text-lg font-bold text-white mb-2">
                What payment methods do you accept?
              </h3>
              <p className="text-gray-400">
                We accept all major credit cards, PayPal, and for Enterprise
                customers, we can arrange bank transfers or other payment methods.
              </p>
            </div>
            <div>
              <h3 className="text-lg font-bold text-white mb-2">
                Is there a long-term commitment?
              </h3>
              <p className="text-gray-400">
                No, our Pro plan is billed monthly and you can cancel anytime.
                Enterprise plans can be customized with annual billing for better
                rates.
              </p>
            </div>
            <div>
              <h3 className="text-lg font-bold text-white mb-2">
                Do you offer refunds?
              </h3>
              <p className="text-gray-400">
                Yes, we offer a 30-day money-back guarantee for Pro subscriptions.
                Enterprise terms are handled on a case-by-case basis.
              </p>
            </div>
          </div>
        </div>

        {/* CTA */}
        <div className="mt-16 text-center">
          <h2 className="text-3xl font-bold text-white mb-8">
            Still Have Questions?
          </h2>
          <p className="text-xl text-gray-300 mb-12 max-w-2xl mx-auto">
            Our team is here to help you find the perfect plan for your needs.
          </p>
          <div className="flex flex-col md:flex-row gap-6 justify-center">
            <a
              href="/contact"
              className="bg-blue-600 hover:bg-blue-700 text-white font-bold py-4 px-8 rounded-lg transition"
            >
              Contact Sales
            </a>
            <a
              href="/docs/pricing"
              className="bg-gray-700 hover:bg-gray-600 text-white font-bold py-4 px-8 rounded-lg transition"
            >
              Read Pricing Guide
            </a>
          </div>
        </div>
      </main>

      {/* Get Started CTA */}
      <section className="bg-gradient-to-r from-blue-600 to-purple-600 py-20">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold text-white mb-8">
            Ready to Transform Your Workflow?
          </h2>
          <p className="text-xl text-white mb-12 max-w-2xl mx-auto">
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
              href="/purchase/pro"
              className="bg-transparent border-2 border-white text-white hover:bg-white/10 font-bold py-4 px-8 rounded-lg transition"
            >
              Go Pro
            </a>
          </div>
        </div>
      </section>
    </div>
  );
}