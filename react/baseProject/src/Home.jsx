import React from 'react';
import { Link } from 'react-router-dom';
import AppLayout from './AppLayout';

function Home() {
  return (
    <AppLayout>
    <div>
      <h1>Home Page</h1>
      <Link to="/contact">Go to Contact</Link>
    </div>
    </AppLayout>
  );
}

export default Home;
