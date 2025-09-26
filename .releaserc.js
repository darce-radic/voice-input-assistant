module.exports = {
  branches: [
    '+([0-9])?(.{+([0-9]),x}).x',
    'main',
    'next',
    'next-major',
    { name: 'beta', prerelease: true },
    { name: 'alpha', prerelease: true },
  ],
  plugins: [
    [
      '@semantic-release/commit-analyzer',
      {
        preset: 'angular',
        releaseRules: [
          { type: 'docs', scope: 'README', release: 'patch' },
          { type: 'refactor', release: 'patch' },
          { type: 'style', release: 'patch' },
          { scope: 'no-release', release: false },
        ],
        parserOpts: {
          noteKeywords: ['BREAKING CHANGE', 'BREAKING CHANGES'],
        },
      },
    ],
    [
      '@semantic-release/release-notes-generator',
      {
        preset: 'angular',
        parserOpts: {
          noteKeywords: ['BREAKING CHANGE', 'BREAKING CHANGES', 'BREAKING'],
        },
        writerOpts: {
          commitsSort: ['subject', 'scope'],
        },
      },
    ],
    [
      '@semantic-release/changelog',
      {
        changelogFile: 'CHANGELOG.md',
      },
    ],
    [
      '@semantic-release/npm',
      {
        npmPublish: false, // Don't publish to npm registry
      },
    ],
    [
      '@semantic-release/git',
      {
        assets: [
          'CHANGELOG.md',
          'package.json',
          'package-lock.json',
          'apps/*/package.json',
          'packages/*/package.json',
        ],
        message:
          'chore(release): ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}',
      },
    ],
    [
      '@semantic-release/github',
      {
        successComment:
          ':tada: This ${issue.pull_request ? "PR is included" : "issue has been resolved"} in version [${nextRelease.version}](${releases.filter(release => /github\.com/.test(release.url))[0].url}) :tada:',
        failTitle: 'The automated release is failing 🚨',
        failComment:
          'The automated release from the `${branch.name}` branch failed. :x:\n\nI recommend you give this issue a high priority, so other packages depending on you can benefit from your bug fixes and new features again.\n\nYou can find below the list of errors reported by **semantic-release**. Each one of them has to be resolved in order to automatically publish your package. I'm sure you can fix this :muscle:.\n\nErrors:\n- ${errors.map(err => err.message).join("\\n- ")}',
        labels: ['semantic-release'],
        assignees: ['@semantic-release-bot'],
        assets: [
          {
            path: 'VoiceInputAssistant-*.zip',
            name: 'VoiceInputAssistant-${nextRelease.version}-portable.zip',
            label: 'Portable Desktop Application',
          },
          {
            path: 'apps/desktop/bin/**/*.msix',
            name: 'VoiceInputAssistant-${nextRelease.version}.msix',
            label: 'MSIX Package',
          },
        ],
      },
    ],
    [
      '@semantic-release/exec',
      {
        prepareCmd: 'echo "Preparing release ${nextRelease.version}"',
        publishCmd: 'echo "Publishing release ${nextRelease.version}"',
        successCmd: 'echo "Successfully released ${nextRelease.version}"',
      },
    ],
  ],
};