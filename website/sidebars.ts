import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docs: [
    'index',
    'introduction',
    'repository-pattern',
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
    {
      type: 'category',
      label: 'The Entity Manager',
      link: {type: 'doc', id: 'entity-manager/README'},
      items: [
        'entity-manager/entity-validation',
        'entity-manager/http-request-cancellation',
        'entity-manager/caching-entities',
        'entity-manager/operation-pipeline',
      ],
    },
    {
      type: 'category',
      label: 'Repository Lifecycle',
      link: {type: 'doc', id: 'repository-lifecycle/README'},
      items: [
        'repository-lifecycle/seeding',
      ],
    },
    {
      type: 'category',
      label: 'User Entities',
      link: {type: 'doc', id: 'user-entities/README'},
      items: [
        'user-entities/user-identifier-resolution',
        'user-entities/automatic-timestamps',
      ],
    },
    'multi-tenancy',
    'soft-delete',
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
        'specifications/README',
      ],
    },
    {
      type: 'category',
      label: 'Health Checks',
      link: {type: 'doc', id: 'health-checks/overview'},
      items: [
        'health-checks/configuration',
        'health-checks/driver-specific',
        'health-checks/advanced-scenarios',
        'health-checks/troubleshooting',
      ],
    },
    {
      type: 'category',
      label: 'Specifications',
      link: {type: 'doc', id: 'specifications/README'},
      items: [],
    },
    'sample-app',
    {
      type: 'category',
      label: 'Migrate',
      items: [
        'migrating-from-1.7',
        'migrating-from-1.7.2',
      ],
    },
    'roadmap',
  ],
};

export default sidebars;
