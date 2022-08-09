<template>
  <ui-drawer viewport-height>
    <ui-drawer-header>
      <img class="logo" src="/gebhardt-logo.svg" />
    </ui-drawer-header>
    <ui-drawer-content>
      <ui-nav>
        <RouterLink to="/">
          <ui-nav-item href="javascript:void(0)">
            <ui-icon>home</ui-icon>
            Home
          </ui-nav-item>
        </RouterLink>
        <RouterLink to="/order">
          <ui-nav-item href="javascript:void(0)">
            <ui-icon>shopping_cart</ui-icon> Orders
          </ui-nav-item>
        </RouterLink>
      </ui-nav>
    </ui-drawer-content>
  </ui-drawer>
</template>
