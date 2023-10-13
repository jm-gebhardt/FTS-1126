# How to start using ADRs

To start using ADRs, talk with your teammates about these areas.

Decision identification:

  * How urgent and how important is the AD?

  * Does it have to be made now, or can it wait until more is known?

  * Both personal and collective experience, as well as recognized design methods and practices, can assist with decision identification.

  * Ideally maintain a decision todo list that complements the product todo list.

Decision making:

  * A number of decision making techniques exists, both general ones and software architecture specific ones, for instance, dialogue mapping.

  * Group decision making is an active research topic.

Decision enactment and enforcement:

  * ADs are used in software design; hence they have to be communicated to, and accepted by, the stakeholders of the system that fund, develop, and operate it.

  * Architecturally evident coding styles and code reviews that focus on architectural concerns and decisions are two related practices.

  * ADs also have to be (re-)considered when modernizing a software system in software evolution.

Decision sharing (optional):

  * Many ADs recur across projects.

  * Hence, experiences with past decisions, both good and bad, can be valuable reusable assets when employing an explicit knowledge management strategy.

  * Group decision making is an active research topic.

Decision documentation:

  * Many templates and tools for decision capturing exist.

  * See agile communities, e.g. M. Nygard's ADRs.

  * See traditional software engineering and architecture design processes, e.g. table layouts suggested by IBM UMF and by Tyree and Akerman from CapitalOne.

Decision guidance:

  * The steps above are adopted from the Wikipedia entry on [Architectural Decision](https://en.wikipedia.org/wiki/Architectural_decision)

  * A number of decision making techniques exists, both general ones and software architecture specific ones, for instance, dialogue mapping.
  
# Suggestions for writing good ADRs

Characteristics of a good ADR:

  * Rational: Explain the reasons for doing the particular AD. This can include the context (see below), pros and cons of various potential choices, feature comparions, cost/benefit discussions, and more.

  * Specific: Each ADR should be about one AD, not multiple ADs.

  * Timestamps: Identify when each item in the ADR is written. This is especially important for aspects that may change over time, such as costs, schedules, scaling, and the like.

  * Immutable: Don't alter existing information in an ADR. Instead, amend the ADR by adding new information, or supersede the ADR by creating a new ADR.

Characteristics of a good "Context" section in an ADR:

  * Explain your organization's situation and business priorities.

  * Include rationale and considerations based on social and skills makeups of your teams.

  * Include pros and cons that are relevant, and describe them in terms that align with your needs and goals.

Characteristics of good "Consequences" section in an ADR:

  * Explain what follows from making the decision. This can include the effects, outcomes, outputs, follow ups, and more.

  * Include information about any subsequent ADRs. It's relatively common for one ADR to trigger the need for more ADRs, such as when one ADR makes a big overarching choice, which in turn creates needs for more smaller decisions.

  * Include any after-action review processes. It's typical for teams to review each ADR one month later, to compare the ADR information with what's happened in actual practice, in order to learn and grow.

A new ADR may take the place of a previous ADR:

  * When an AD is made that replaces or invalidates a previous ADR, then a new ADR should be created

# Using the ADR tool

A package to provide adr-tool to python is available at https://pypi.org/project/adr-tools-python/ and should be used for creating new ADRs.
