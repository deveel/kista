import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docs: [
    'introduction',
    'repository-pattern',
    'index',
    {
      type: 'category',
      label: 'Customize the Repository',
      link: {type: 'doc', id: 'custom-repository/README'},
      items: [
        'custom-repository/design',
        'custom-repository/implementation',
        'custom-repository/query-methods',
        'custom-repository/registration',
      ],
    },
    'repository-lifecycle',
    'user-entities',
    'multi-tenancy',
    {
      type: 'category',
      label: 'Repository Implementations',
      link: {type: 'doc', id: 'repository-implementations/README'},
      items: [
        'repository-implementations/in-memory',
        'repository-implementations/ef-core',
        'repository-implementations/mongodb',
      ],
    },
    {
      type: 'category',
      label: 'Filtering',
      link: {type: 'doc', id: 'filtering/index'},
      items: [
        'filtering/filter-cache',
      ],
    },
    {
      type: 'category',
      label: 'The Entity Manager',
      link: {type: 'doc', id: 'entity-manager/README'},
      items: [
        'entity-manager/entity-validation',
        'entity-manager/http-request-cancellation',
        'entity-manager/caching-entities',
      ],
    },
    'sample-app',
    'roadmap',
  ],
};

export default sidebars;
