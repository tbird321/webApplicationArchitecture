import React from 'react';
import AppBar from '@mui/material/AppBar';
import Toolbar from '@mui/material/Toolbar';
import IconButton from '@mui/material/IconButton';
import MenuIcon from '@mui/icons-material/Menu';

function AppHeader({ onMenuIconClick }) {
  return (
    <AppBar position="static">
      <Toolbar>
        <IconButton
          edge="start"
          color="inherit"
          aria-label="menu"
          onClick={onMenuIconClick}
        >
          <MenuIcon />
        </IconButton>
        {/* Add other header content, e.g., app title or user info */}
      </Toolbar>
    </AppBar>
  );
}

export default AppHeader;
