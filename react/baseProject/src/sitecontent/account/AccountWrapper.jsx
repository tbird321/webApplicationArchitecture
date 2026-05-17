import React from 'react';
import { ResetPassword } from '@tbirdcomponents/reactcomponents';
import { Auth } from 'aws-amplify';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly



function AccountWrapper({ onShowAccount }) {
    const handleShowAccount = (bool) => {
        onShowAccount(bool);
    };

    const handleChangeAccountPassword = async (currentPassword, newPassword) => {
        try {
            const user = await Auth.currentAuthenticatedUser(); // Correctly retrieves the current authenticated user
            await Auth.changePassword(user, currentPassword, newPassword);
            handleShowAccount(false);
            console.log('Password changed successfully');
            // Handle successful password change (e.g., notify the user)
        } catch (err) {
            console.error('Error changing password:', err);
            // Handle errors (e.g., incorrect old password, password complexity requirements not met)
        }
    };

    return (
        <>
            <ResetPassword onChangeAccountPassword={handleChangeAccountPassword} />
        </>
    );
}

export default AccountWrapper;