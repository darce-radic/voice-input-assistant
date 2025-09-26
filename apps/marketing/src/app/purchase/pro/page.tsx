import { FC } from 'react';

export default function ProPurchase() {
  const annualDiscount = 20; // 20% off for annual
  const monthlyPrice = 9.99;
  const annualPrice = monthlyPrice * 12 * (1 - annualDiscount / 100);

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-900 to-gray-800">
      <main className="container mx-auto px-4 py-20">
        <div className="max-w-3xl mx-auto">
          {/* Header */}
          <div className="text-center mb-12">
            <h1 className="text-4xl font-bold text-white mb-4">
              Upgrade to Voice Input Assistant Pro
            </h1>
            <p className="text-xl text-gray-300">
              Get access to advanced features and cloud processing
            </p>
          </div>

          {/* Billing Period Selection */}
          <div className="bg-gray-800 rounded-lg p-8 mb-8">
            <h2 className="text-2xl font-bold text-white mb-6">
              Choose Billing Period
            </h2>
            <div className="grid md:grid-cols-2 gap-6">
              {/* Monthly Plan */}
              <label className="relative cursor-pointer">
                <input
                  type="radio"
                  name="billing"
                  className="peer sr-only"
                  defaultChecked
                />
                <div className="p-6 bg-gray-700 rounded-lg peer-checked:ring-2 peer-checked:ring-blue-500 hover:bg-gray-650">
                  <div className="flex justify-between items-start mb-4">
                    <div>
                      <h3 className="text-lg font-bold text-white">Monthly</h3>
                      <p className="text-gray-400">Pay month to month</p>
                    </div>
                    <div className="text-right">
                      <div className="text-2xl font-bold text-white">
                        ${monthlyPrice}
                      </div>
                      <div className="text-sm text-gray-400">/month</div>
                    </div>
                  </div>
                  <ul className="text-gray-300 space-y-2">
                    <li className="flex items-center">
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
                      Cancel anytime
                    </li>
                    <li className="flex items-center">
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
                      All Pro features included
                    </li>
                  </ul>
                </div>
              </label>

              {/* Annual Plan */}
              <label className="relative cursor-pointer">
                <input type="radio" name="billing" className="peer sr-only" />
                <div className="p-6 bg-gray-700 rounded-lg peer-checked:ring-2 peer-checked:ring-blue-500 hover:bg-gray-650">
                  <div className="flex justify-between items-start mb-4">
                    <div>
                      <h3 className="text-lg font-bold text-white">Annual</h3>
                      <p className="text-gray-400">
                        {annualDiscount}% discount
                      </p>
                    </div>
                    <div className="text-right">
                      <div className="text-2xl font-bold text-white">
                        ${(annualPrice / 12).toFixed(2)}
                      </div>
                      <div className="text-sm text-gray-400">
                        /month, billed annually
                      </div>
                    </div>
                  </div>
                  <ul className="text-gray-300 space-y-2">
                    <li className="flex items-center">
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
                      Save ${(monthlyPrice * 12 - annualPrice).toFixed(2)}/year
                    </li>
                    <li className="flex items-center">
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
                      All Pro features included
                    </li>
                  </ul>
                </div>
              </label>
            </div>
          </div>

          {/* Payment Form */}
          <div className="bg-gray-800 rounded-lg p-8 mb-8">
            <h2 className="text-2xl font-bold text-white mb-6">
              Payment Details
            </h2>
            <form className="space-y-6">
              <div>
                <label className="block text-sm font-bold text-gray-300 mb-2">
                  Card Information
                </label>
                <div className="bg-gray-700 p-4 rounded-lg space-y-4">
                  {/* Card Number */}
                  <div>
                    <label className="sr-only">Card number</label>
                    <input
                      type="text"
                      placeholder="Card number"
                      className="w-full bg-gray-600 text-white px-4 py-2 rounded border border-gray-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                    />
                  </div>
                  <div className="grid grid-cols-3 gap-4">
                    {/* Expiry */}
                    <div className="col-span-2">
                      <label className="sr-only">Expiration date</label>
                      <input
                        type="text"
                        placeholder="MM / YY"
                        className="w-full bg-gray-600 text-white px-4 py-2 rounded border border-gray-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                      />
                    </div>
                    {/* CVC */}
                    <div>
                      <label className="sr-only">CVC</label>
                      <input
                        type="text"
                        placeholder="CVC"
                        className="w-full bg-gray-600 text-white px-4 py-2 rounded border border-gray-500 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Billing Address */}
              <div>
                <label className="block text-sm font-bold text-gray-300 mb-2">
                  Billing Address
                </label>
                <div className="space-y-4">
                  <input
                    type="text"
                    placeholder="Full name"
                    className="w-full bg-gray-700 text-white px-4 py-2 rounded border border-gray-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                  />
                  <input
                    type="text"
                    placeholder="Address line 1"
                    className="w-full bg-gray-700 text-white px-4 py-2 rounded border border-gray-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                  />
                  <input
                    type="text"
                    placeholder="Address line 2 (optional)"
                    className="w-full bg-gray-700 text-white px-4 py-2 rounded border border-gray-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                  />
                  <div className="grid grid-cols-2 gap-4">
                    <input
                      type="text"
                      placeholder="City"
                      className="w-full bg-gray-700 text-white px-4 py-2 rounded border border-gray-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                    />
                    <input
                      type="text"
                      placeholder="ZIP / Postal code"
                      className="w-full bg-gray-700 text-white px-4 py-2 rounded border border-gray-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                    />
                  </div>
                  <select className="w-full bg-gray-700 text-white px-4 py-2 rounded border border-gray-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500">
                    <option value="">Select country</option>
                    <option value="US">United States</option>
                    <option value="CA">Canada</option>
                    <option value="GB">United Kingdom</option>
                    <option value="AU">Australia</option>
                  </select>
                </div>
              </div>

              {/* Email */}
              <div>
                <label className="block text-sm font-bold text-gray-300 mb-2">
                  Email Address
                </label>
                <input
                  type="email"
                  placeholder="email@example.com"
                  className="w-full bg-gray-700 text-white px-4 py-2 rounded border border-gray-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                />
              </div>

              {/* Terms */}
              <div className="flex items-start">
                <input
                  type="checkbox"
                  id="terms"
                  className="mt-1 bg-gray-700 border-gray-600 rounded focus:ring-blue-500"
                />
                <label htmlFor="terms" className="ml-2 text-sm text-gray-300">
                  I agree to the{" "}
                  <a href="/terms" className="text-blue-400 hover:text-blue-300">
                    Terms of Service
                  </a>{" "}
                  and{" "}
                  <a
                    href="/privacy"
                    className="text-blue-400 hover:text-blue-300"
                  >
                    Privacy Policy
                  </a>
                </label>
              </div>

              {/* Submit */}
              <button
                type="submit"
                className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 px-6 rounded-lg transition"
              >
                Complete Purchase
              </button>
            </form>
          </div>

          {/* Guarantee */}
          <div className="text-center">
            <div className="flex items-center justify-center mb-4">
              <svg
                className="w-6 h-6 text-green-400 mr-2"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                />
              </svg>
              <span className="text-white font-bold">
                30-Day Money-Back Guarantee
              </span>
            </div>
            <p className="text-gray-400 text-sm max-w-md mx-auto">
              Try Voice Input Assistant Pro risk-free. If you're not completely
              satisfied, we'll refund your purchase within 30 days.
            </p>
          </div>
        </div>
      </main>

      {/* Need Help */}
      <section className="bg-gray-800 py-12 border-t border-gray-700">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-2xl font-bold text-white mb-4">Need Help?</h2>
          <p className="text-gray-300 mb-6">
            Our support team is here to assist you with your purchase
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <a
              href="/docs/pro"
              className="text-white hover:text-blue-400 transition"
            >
              Read Pro Guide
            </a>
            <span className="hidden sm:inline text-gray-600">•</span>
            <a
              href="/contact"
              className="text-white hover:text-blue-400 transition"
            >
              Contact Support
            </a>
            <span className="hidden sm:inline text-gray-600">•</span>
            <a href="/faq" className="text-white hover:text-blue-400 transition">
              View FAQs
            </a>
          </div>
        </div>
      </section>
    </div>
  );
}