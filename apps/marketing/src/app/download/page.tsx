import Image from 'next/image';

export default function Download() {
  const systemRequirements = [
    'Windows 10 or 11 (64-bit)',
    '4GB RAM minimum',
    '1GB free disk space',
    'Microphone',
    'Internet connection (for cloud features)'
  ];

  const versions = [
    {
      version: 'v1.2.0',
      date: 'Sept 25, 2024',
      type: 'latest',
      changes: [
        'Added adaptive learning system',
        'Improved transcription accuracy',
        'New user interface',
        'Bug fixes and performance improvements'
      ]
    },
    {
      version: 'v1.1.0',
      date: 'Aug 15, 2024',
      type: 'stable',
      changes: [
        'Added cloud processing support',
        'New application profiles',
        'Improved offline mode',
        'Fixed memory usage issues'
      ]
    }
  ];

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-900 to-gray-800">
      <main className="container mx-auto px-4 py-20">
        {/* Hero */}
        <div className="text-center mb-16">
          <h1 className="text-4xl md:text-6xl font-bold text-white mb-6">
            Download Voice Input Assistant
          </h1>
          <p className="text-xl text-gray-300 mb-12 max-w-3xl mx-auto">
            Get started with the most advanced voice-to-text solution for Windows.
            Choose between our desktop app or web-based solution.
          </p>
        </div>

        {/* Download Options */}
        <div className="grid md:grid-cols-2 gap-8 mb-16">
          {/* Desktop App */}
          <div className="bg-gray-800 rounded-lg p-8">
            <div className="flex items-center mb-6">
              <div className="bg-blue-600 rounded-full p-3 mr-4">
                <svg
                  className="w-6 h-6 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                  />
                </svg>
              </div>
              <div>
                <h3 className="text-xl font-bold text-white">Desktop App</h3>
                <p className="text-gray-400">Windows 10/11 (64-bit)</p>
              </div>
            </div>
            <div className="space-y-4 mb-8">
              <a
                href="/downloads/VoiceInputAssistant-1.2.0-x64.exe"
                className="block w-full bg-blue-600 hover:bg-blue-700 text-white text-center font-bold py-3 px-6 rounded-lg transition"
              >
                Download for Windows (64-bit)
              </a>
              <div className="text-center text-gray-400 text-sm">
                Version 1.2.0 (Latest) • 45MB
              </div>
            </div>
            <div className="border-t border-gray-700 pt-6">
              <h4 className="text-white font-bold mb-2">System Requirements:</h4>
              <ul className="text-gray-400 space-y-2">
                {systemRequirements.map((req, index) => (
                  <li key={index} className="flex items-center">
                    <svg
                      className="w-4 h-4 mr-2 text-green-400"
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
                    {req}
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Web App */}
          <div className="bg-gray-800 rounded-lg p-8">
            <div className="flex items-center mb-6">
              <div className="bg-purple-600 rounded-full p-3 mr-4">
                <svg
                  className="w-6 h-6 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9"
                  />
                </svg>
              </div>
              <div>
                <h3 className="text-xl font-bold text-white">Web App</h3>
                <p className="text-gray-400">Use in your browser</p>
              </div>
            </div>
            <div className="space-y-4 mb-8">
              <a
                href="https://app.voiceinputassistant.com"
                className="block w-full bg-purple-600 hover:bg-purple-700 text-white text-center font-bold py-3 px-6 rounded-lg transition"
              >
                Launch Web App
              </a>
              <div className="text-center text-gray-400 text-sm">
                Always up to date • No installation required
              </div>
            </div>
            <div className="border-t border-gray-700 pt-6">
              <h4 className="text-white font-bold mb-2">Supported Browsers:</h4>
              <ul className="text-gray-400 space-y-2">
                <li>Google Chrome</li>
                <li>Mozilla Firefox</li>
                <li>Microsoft Edge</li>
                <li>Safari 14+</li>
              </ul>
            </div>
          </div>
        </div>

        {/* Release Notes */}
        <div className="bg-gray-800 rounded-lg p-8">
          <h2 className="text-2xl font-bold text-white mb-8">Release Notes</h2>
          <div className="space-y-8">
            {versions.map((version, index) => (
              <div key={index} className="border-b border-gray-700 last:border-0 pb-8 last:pb-0">
                <div className="flex items-center gap-4 mb-4">
                  <h3 className="text-xl font-bold text-white">{version.version}</h3>
                  <span className="text-gray-400">•</span>
                  <span className="text-gray-400">{version.date}</span>
                  {version.type === 'latest' && (
                    <span className="bg-blue-600 text-white text-sm px-3 py-1 rounded-full">
                      Latest
                    </span>
                  )}
                  {version.type === 'stable' && (
                    <span className="bg-green-600 text-white text-sm px-3 py-1 rounded-full">
                      Stable
                    </span>
                  )}
                </div>
                <ul className="space-y-2 text-gray-300">
                  {version.changes.map((change, changeIndex) => (
                    <li key={changeIndex} className="flex items-start">
                      <span className="text-blue-400 mr-2">•</span>
                      {change}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>

        {/* FAQ */}
        <div className="mt-16">
          <h2 className="text-2xl font-bold text-white mb-8">
            Frequently Asked Questions
          </h2>
          <div className="grid md:grid-cols-2 gap-8">
            <div className="space-y-6">
              <div>
                <h3 className="text-lg font-bold text-white mb-2">
                  Which version should I choose?
                </h3>
                <p className="text-gray-400">
                  Choose the desktop app for the best performance and system-wide
                  integration. Use the web app if you prefer no installation or
                  need cross-platform support.
                </p>
              </div>
              <div>
                <h3 className="text-lg font-bold text-white mb-2">
                  Is it free to use?
                </h3>
                <p className="text-gray-400">
                  Yes! Both versions offer a free tier with basic features. Pro and
                  Enterprise plans are available for advanced features and
                  commercial use.
                </p>
              </div>
            </div>
            <div className="space-y-6">
              <div>
                <h3 className="text-lg font-bold text-white mb-2">
                  Can I use it offline?
                </h3>
                <p className="text-gray-400">
                  The desktop app supports full offline mode with local processing.
                  The web app requires an internet connection.
                </p>
              </div>
              <div>
                <h3 className="text-lg font-bold text-white mb-2">
                  How do I get updates?
                </h3>
                <p className="text-gray-400">
                  The desktop app includes automatic updates. The web app is always
                  up to date when you access it.
                </p>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Download CTA */}
      <section className="bg-gradient-to-r from-blue-600 to-purple-600 py-20">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold text-white mb-8">
            Ready to Get Started?
          </h2>
          <p className="text-xl text-white mb-12 max-w-2xl mx-auto">
            Download Voice Input Assistant now and experience the future of
            voice-to-text technology.
          </p>
          <div className="flex flex-col md:flex-row gap-6 justify-center">
            <a
              href="/downloads/VoiceInputAssistant-1.2.0-x64.exe"
              className="bg-white text-blue-600 hover:bg-gray-100 font-bold py-4 px-8 rounded-lg transition"
            >
              Download for Windows
            </a>
            <a
              href="https://app.voiceinputassistant.com"
              className="bg-transparent border-2 border-white text-white hover:bg-white/10 font-bold py-4 px-8 rounded-lg transition"
            >
              Try Web Version
            </a>
          </div>
        </div>
      </section>
    </div>
  );
}