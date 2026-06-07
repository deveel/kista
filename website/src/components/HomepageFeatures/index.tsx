import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  Svg: React.ComponentType<React.ComponentProps<'svg'>>;
  description: React.JSX.Element;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Multi-Driver Repository Pattern',
    Svg: require('@site/static/img/feature-drivers.svg').default,
    description: (
      <>
        Abstract your data layer across EF Core, MongoDB, In-Memory, and DynamicLinq with a single, consistent API &mdash; switch drivers without changing application code.
      </>
    ),
  },
  {
    title: 'Entity Lifecycle Management',
    Svg: require('@site/static/img/feature-lifecycle.svg').default,
    description: (
      <>
        Validation, caching, state machines, and audit trails through a unified EntityManager &mdash; no boilerplate, no scattered infrastructure code.
      </>
    ),
  },
  {
    title: 'DDD-First Architecture',
    Svg: require('@site/static/img/feature-ddd.svg').default,
    description: (
      <>
        Domain-oriented interfaces, multi-tenancy, user scoping, and OpenTelemetry integration out of the box &mdash; built for real-world applications.
      </>
    ),
  },
];

function Feature({ title, Svg, description }: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
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
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
