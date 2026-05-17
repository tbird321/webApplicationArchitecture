import React from 'react';
import Drawer from '@mui/material/Drawer';

function LeftDrawer({ open, onClose }) {
  return (
    <Drawer open={open} onClose={onClose}>
      {/* Add your drawer content here */}
    </Drawer>
  );
}

export default LeftDrawer;
