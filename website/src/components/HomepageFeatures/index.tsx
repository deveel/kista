import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

function DatabaseIcon() {
  return (
    <svg
      className={styles.featureSvg}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <ellipse cx="12" cy="5" rx="9" ry="3" />
      <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3" />
      <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5" />
    </svg>
  );
}

function LifecycleIcon() {
  return (
    <svg
      className={styles.featureSvg}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
      <polyline points="9 12 11 14 15 10" />
    </svg>
  );
}

function DomainIcon() {
  return (
    <svg
      className={styles.featureSvg}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
      <line x1="8" y1="21" x2="16" y2="21" />
      <line x1="12" y1="17" x2="12" y2="21" />
    </svg>
  );
}

type FeatureItem = {
  title: string;
  Icon: React.ComponentType;
  description: React.JSX.Element;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Multi-Driver Repository Pattern',
    Icon: DatabaseIcon,
    description: (
      <>
        Abstract your data layer across EF Core, MongoDB, In-Memory, and DynamicLinq with a single, consistent API &mdash; switch drivers without changing application code.
      </>
    ),
  },
  {
    title: 'Entity Lifecycle Management',
    Icon: LifecycleIcon,
    description: (
      <>
        Validation, caching, state machines, and audit trails through a unified EntityManager &mdash; no boilerplate, no scattered infrastructure code.
      </>
    ),
  },
  {
    title: 'DDD-First Architecture',
    Icon: DomainIcon,
    description: (
      <>
        Domain-oriented interfaces, multi-tenancy, user scoping, and OpenTelemetry integration out of the box &mdash; built for real-world applications.
      </>
    ),
  },
];

function Feature({ title, Icon, description }: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Icon />
      </div>
      <div className="text--center padding-horiz--md">
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): React.JSX.Element {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props) => (
            <Feature key={props.title} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
