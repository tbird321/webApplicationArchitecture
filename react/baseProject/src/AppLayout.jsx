import React, { useState } from 'react';
import AppHeader from './AppHeader'; // Your Header component
import AppMenu from './AppMenu'; // Your AppMenu component
import LeftDrawer from './LeftDrawer'; // Your LeftDrawer component

function Layout({ children, isLoggedIn }) {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  const handleMenuIconClick = () => {
    setIsDrawerOpen(!isDrawerOpen);
  };

  const handleDrawerClose = () => {
    setIsDrawerOpen(false);
  };

  return (
    <div>
      <LeftDrawer open={isDrawerOpen} onClose={handleDrawerClose} />
      <main>
       <AppMenu></AppMenu>
        {children}
      </main>
    </div>
  );
}

export default Layout;
