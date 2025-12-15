---
layout: default
title: Sitemap
description: A complete index of all documentation pages on this site.
keywords: Storage sitemap, documentation index, features, ADR, API, setup, credentials, testing
permalink: /sitemap/
nav_order: 99
---

{% assign effective_baseurl = site.baseurl %}
{% if site.url != nil and site.url != '' %}
{% unless site.url contains 'github.io' %}
{% assign effective_baseurl = '' %}
{% endunless %}
{% endif %}

# Sitemap

This page lists the docs sections and every generated page (Features, ADRs, API) so it’s always easy to find content.

## Main pages

- [Home]({{ effective_baseurl }}/)
- [Setup]({{ effective_baseurl }}/setup/)
- [Credentials]({{ effective_baseurl }}/credentials/)
- [Testing]({{ effective_baseurl }}/testing/)
- [Features]({{ effective_baseurl }}/features/)
- [ADR]({{ effective_baseurl }}/adr/)
- [API]({{ effective_baseurl }}/api/)
- [GitHub](https://github.com/{{ site.github_repo }})

## Features

{% assign feature_pages = site.pages | where_exp: "p", "p.url contains '/features/' and p.url contains '.html'" | sort: "title" %}
{% for p in feature_pages %}
- [{{ p.title | escape }}]({{ effective_baseurl }}{{ p.url }})
{% endfor %}

## ADR

{% assign adr_pages = site.pages | where_exp: "p", "p.url contains '/adr/' and p.url contains '.html'" | sort: "title" %}
{% for p in adr_pages %}
- [{{ p.title | escape }}]({{ effective_baseurl }}{{ p.url }})
{% endfor %}

## API

{% assign api_pages = site.pages | where_exp: "p", "p.url contains '/api/' and p.url contains '.html'" | sort: "title" %}
{% for p in api_pages %}
- [{{ p.title | escape }}]({{ effective_baseurl }}{{ p.url }})
{% endfor %}

## Machine sitemap

- XML sitemap: [sitemap.xml]({{ effective_baseurl }}/sitemap.xml)
