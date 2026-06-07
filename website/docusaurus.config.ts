import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Kista',
  tagline: 'A pragmatic, DDD-aligned data access framework for .NET',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://kista.deveel.org',
  baseUrl: '/',

  organizationName: 'deveel',
  projectName: 'kista',

  onBrokenLinks: 'throw',

  headTags: [
    {
      tagName: 'meta',
      attributes: {
        name: 'algolia-site-verification',
        content: '7E0BBB65DCBBD694',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'apple-touch-icon',
        sizes: '180x180',
        href: '/img/apple-touch-icon.png',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'icon',
        type: 'image/png',
        sizes: '32x32',
        href: '/img/icon-32.png',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'icon',
        type: 'image/png',
        sizes: '192x192',
        href: '/img/icon-192.png',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'icon',
        type: 'image/png',
        sizes: '512x512',
        href: '/img/icon-512.png',
      },
    },
  ],

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs',
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/deveel/kista/edit/main/docs/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/kista-full-logo.png',
    algolia: {
      appId: 'JYSR40O1I0',
      apiKey: '479261bc4e1a490dec72ff2d553c229b',
      indexName: 'kista',
      contextualSearch: true,
    },
    colorMode: {
      defaultMode: 'light',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
    navbar: {
      logo: {
        alt: 'Kista Logo',
        src: 'img/kista-full-logo.png',
        width: 106,
        height: 45,
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docs',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'doc',
          docsPluginId: 'default',
          position: 'left',
          label: 'Getting Started',
          docId: 'index',
        },
        {
          type: 'html',
          position: 'right',
          value: '<span class="header-right-group"><a class="header-github-link" href="https://github.com/deveel/kista" aria-label="GitHub repository" target="_blank" rel="noopener noreferrer"></a><a class="header-deveel-link" href="https://deveel.org" aria-label="Deveel website" target="_blank" rel="noopener noreferrer"><img src="/img/deveel-logo.svg" alt="Deveel" class="header-deveel-logo" /></a></span>',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/',
            },
            {
              label: 'Repository Pattern',
              to: '/docs/repository-pattern',
            },
            {
              label: 'Roadmap',
              to: '/docs/roadmap',
            },
          ],
        },
        {
          title: 'Packages',
          items: [
            {
              label: 'NuGet',
              href: 'https://www.nuget.org/profiles/deveel',
            },
            {
              label: 'GitHub Packages',
              href: 'https://github.com/deveel/kista/packages',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/deveel/kista',
            },
            {
              label: 'Issues',
              href: 'https://github.com/deveel/kista/issues',
            },
            {
              label: 'Deveel',
              href: 'https://deveel.org',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Antonello Provenzano.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp'],
    },
  } satisfies Preset.ThemeConfig,

  plugins: [
    [
      '@docusaurus/plugin-client-redirects',
      {
        redirects: [],
      },
    ],
  ],
};

export default config;
